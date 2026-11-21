"""Check version gates and that invalid/private payloads cannot be released."""
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import zipfile

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("uei_ci", REPO / "scripts/ci.py")
ci = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ci)
COMMIT = "a" * 40


class AutomationChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="uei-ci-check-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / "repo"
        self.root.mkdir()
        self.set_version("1.2.4")
        for name in ("packaging/README.md", "packaging/RELEASE-NOTES.md", "packaging/icon.png", "LICENSE"):
            destination = self.root / name
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(REPO / name, destination)
        (self.root / "packaging/RELEASE-NOTES.md").write_text(
            "<!-- version: 1.2.4 -->\n\n## Changes\n\nUEI 1.2.4 fixture release notes.\n", encoding="utf-8")
        (self.root / ci.REFERENCE_DIR).mkdir(parents=True)
        self.manifest = {"format": 1, "files": {name: {"size": 2, "sha256": ci.digest(b"MZ")}
                                                     for name in ci.REFERENCES}}
        self.write_manifest()
        self.dll = Path(self.temp.name) / "build/UEI.dll"
        self.dll.parent.mkdir()
        self.dll.write_bytes(b"MZ-test-plugin")
        self.output = Path(self.temp.name) / "release"
        self.commit_patch = patch.object(ci, "source_commit", return_value=COMMIT)
        self.commit_patch.start()
        self.addCleanup(self.commit_patch.stop)

    def set_version(self, version, plugin_version="1.2.4"):
        (self.root / "UEI.csproj").write_text(
            f"<Project><PropertyGroup><Version>{version}</Version></PropertyGroup></Project>", encoding="utf-8")
        base, _, suffix = plugin_version.rpartition(".")
        (self.root / "Plugin.cs").write_text(
            f'const string PluginVersionBase = "{base}";\nconst string PluginVersion = PluginVersionBase + ".{suffix}";',
            encoding="utf-8")

    def write_manifest(self):
        (self.root / ci.REFERENCE_DIR / "manifest.json").write_text(json.dumps(self.manifest), encoding="utf-8")

    def make_package(self):
        ci.package(self.dll, self.output, self.root)

    def refresh_checksums(self):
        names = ["UEI-1.2.4.zip", "UEI.dll", "build-info.json"]
        (self.output / "SHA256SUMS.txt").write_text("".join(
            ci.digest((self.output / name).read_bytes()) + "  " + name + "\n" for name in names), encoding="utf-8")

    def test_valid_version_and_tag(self):
        self.assertEqual(ci.read_version(self.root, "v1.2.4"), "1.2.4")

    def test_reject_version_suffix_and_leading_zeros(self):
        for value in ("1.2", "1.2.4.0", "1.2.4-beta", "01.2.4", "v1.2.4"):
            with self.subTest(value=value):
                self.set_version(value)
                with self.assertRaises(ValueError):
                    ci.read_version(self.root)

    def test_reject_plugin_project_mismatch(self):
        self.set_version("1.2.5")
        with self.assertRaises(ValueError):
            ci.read_version(self.root)

    def test_reject_stale_release_notes_version(self):
        self.set_version("1.2.5", "1.2.5")
        with self.assertRaises(ValueError):
            ci.read_release_notes(self.root)

    def test_reject_empty_release_notes(self):
        (self.root / "packaging/RELEASE-NOTES.md").write_text("<!-- version: 1.2.4 -->\n", encoding="utf-8")
        with self.assertRaises(ValueError):
            ci.read_release_notes(self.root)

    def test_version_marker_is_not_published(self):
        notes = ci.read_release_notes(self.root)
        self.assertNotIn("<!-- version:", notes)
        self.assertIn("1.2.4", notes)

    def test_reject_changed_release_notes_artifact(self):
        self.make_package()
        (self.output / "release-notes.md").write_text("wrong release notes", encoding="utf-8")
        with self.assertRaises(ValueError):
            ci.verify_package(self.output, self.root)

    def test_reject_tag_mismatch(self):
        for tag in ("v1.2.5", "v1.2.4-beta", "V1.2.4", "1.2.4", "v1.2.4; echo injected"):
            with self.subTest(tag=tag), self.assertRaises(ValueError):
                ci.read_version(self.root, tag)

    def test_package_contains_only_uei_and_install_metadata(self):
        self.make_package()
        with zipfile.ZipFile(self.output / "UEI-1.2.4.zip") as archive:
            self.assertEqual([name for name in archive.namelist() if name.endswith(".dll")],
                             ["BepInEx/plugins/UEI/UEI.dll"])
            self.assertIn(b"1.2.4", archive.read("README.md"))
            self.assertNotIn(b"@VERSION@", archive.read("README.md"))
            self.assertEqual(archive.read("BepInEx/plugins/UEI/UEI.dll"), self.dll.read_bytes())
            self.assertEqual(archive.read("icon.png"), (self.root / "packaging/icon.png").read_bytes())
            self.assertEqual(archive.read("CHANGELOG.md").decode("utf-8"),
                             "# 1.2.4\n\n" + ci.read_release_notes(self.root))

    def test_reject_changed_icon_or_changelog_even_with_valid_checksums(self):
        for name in ("icon.png", "CHANGELOG.md"):
            with self.subTest(name=name):
                self.make_package()
                path = self.output / "UEI-1.2.4.zip"
                with zipfile.ZipFile(path) as archive:
                    files = {entry: archive.read(entry) for entry in archive.namelist()}
                files[name] = b"different presentation asset"
                with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
                    for entry, data in files.items():
                        archive.writestr(entry, data)
                self.refresh_checksums()
                with self.assertRaises(ValueError):
                    ci.verify_package(self.output, self.root)

    def test_reject_extra_dll_in_release_directory(self):
        self.make_package()
        (self.output / "Assembly-CSharp.dll").write_bytes(b"private")
        with self.assertRaises(ValueError):
            ci.verify_package(self.output, self.root)

    def test_reject_game_or_fixture_inside_zip_even_with_valid_checksums(self):
        for extra in ("Assembly-CSharp.dll", "UEI.TestFixture.dll", "../outside.txt"):
            with self.subTest(extra=extra):
                self.make_package()
                with zipfile.ZipFile(self.output / "UEI-1.2.4.zip", "a") as archive:
                    archive.writestr(extra, b"must not ship")
                self.refresh_checksums()
                with self.assertRaises(ValueError):
                    ci.verify_package(self.output, self.root)

    def test_reject_changed_asset(self):
        self.make_package()
        (self.output / "UEI.dll").write_bytes(b"tampered")
        with self.assertRaises(ValueError):
            ci.verify_package(self.output, self.root)

    def test_reject_different_standalone_dll_even_with_valid_checksums(self):
        self.make_package()
        (self.output / "UEI.dll").write_bytes(b"other build")
        self.refresh_checksums()
        with self.assertRaises(ValueError):
            ci.verify_package(self.output, self.root)

    def test_reject_wrong_commit_provenance(self):
        self.make_package()
        p = self.output / "build-info.json"
        data = json.loads(p.read_text(encoding="utf-8"))
        data["source_commit"] = "b" * 40
        p.write_text(json.dumps(data), encoding="utf-8")
        self.refresh_checksums()
        with self.assertRaises(ValueError):
            ci.verify_package(self.output, self.root)

    def test_reject_duplicate_checksum(self):
        self.make_package()
        p = self.output / "SHA256SUMS.txt"
        content = p.read_text(encoding="utf-8")
        p.write_text(content + content.splitlines()[0] + "\n", encoding="utf-8")
        with self.assertRaises(ValueError):
            ci.verify_package(self.output, self.root)

    def test_reject_missing_checksum(self):
        self.make_package()
        p = self.output / "SHA256SUMS.txt"
        p.write_text(p.read_text(encoding="utf-8").splitlines()[0] + "\n", encoding="utf-8")
        with self.assertRaises(ValueError):
            ci.verify_package(self.output, self.root)

    def test_reject_invalid_package_icon(self):
        (self.root / "packaging/icon.png").write_bytes(b"missing icon")
        with self.assertRaises(ValueError):
            ci.package(self.dll, self.output, self.root)

    def test_missing_decryption_key_fails(self):
        with patch.dict(os.environ, {}, clear=True), self.assertRaises(ValueError):
            ci.decrypt_references(Path(self.temp.name) / "references", self.root)

    def test_changed_build_reference_fails(self):
        game = Path(self.temp.name) / "game"
        for name in ci.REFERENCES:
            p = game / name
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_bytes(b"MZ")
        ci.verify_references(game, self.root)
        (game / ci.REFERENCES[0]).write_bytes(b"wrong library")
        with self.assertRaises(ValueError):
            ci.verify_references(game, self.root)

    def test_unexpected_reference_manifest_fails(self):
        self.manifest["files"]["unexpected.dll"] = {"size": 2, "sha256": ci.digest(b"MZ")}
        self.write_manifest()
        with self.assertRaises(ValueError):
            ci.reference_manifest(self.root)

    def test_failed_upload_cannot_publish_release(self):
        self.make_package()
        def fail_upload(args, **kwargs):
            if args[:3] == ["gh", "release", "create"]:
                raise subprocess.CalledProcessError(1, args)
            return subprocess.CompletedProcess(args, 0, stdout=COMMIT + "\n")
        with patch.object(ci, "run", side_effect=fail_upload) as runner:
            with self.assertRaises(subprocess.CalledProcessError):
                ci.publish(self.output, "v1.2.4", self.root)
            self.assertFalse(any(call.args[0][:3] == ["gh", "release", "edit"] for call in runner.call_args_list))

    def test_tag_outside_main_cannot_create_release(self):
        self.make_package()
        def reject_ancestor(args, **kwargs):
            if args[:2] == ["git", "merge-base"]:
                raise subprocess.CalledProcessError(1, args)
            return subprocess.CompletedProcess(args, 0, stdout=COMMIT + "\n")
        with patch.object(ci, "run", side_effect=reject_ancestor) as runner:
            with self.assertRaises(subprocess.CalledProcessError):
                ci.publish(self.output, "v1.2.4", self.root)
            self.assertFalse(any(call.args[0][0] == "gh" for call in runner.call_args_list))


if __name__ == "__main__":
    unittest.main()
