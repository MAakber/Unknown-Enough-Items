"""UEI CI helpers. Release archives never contain game/reference assemblies."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import shutil
import struct
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
REFERENCE_DIR = Path(".github/build-references")
SECRET = "UEI_BUILD_REFERENCES_KEY"
REPOSITORY = "MAakber/Unknown-Enough-Items"
REFERENCES = (
    "BepInEx/core/BepInEx.dll",
    "BepInEx/core/0Harmony.dll",
    *("CasualtiesUnknown_Data/Managed/" + name + ".dll" for name in (
        "Assembly-CSharp", "Unity.TextMeshPro", "UnityEngine",
        "UnityEngine.CoreModule", "UnityEngine.InputLegacyModule",
        "UnityEngine.TextRenderingModule", "UnityEngine.UI", "UnityEngine.UIModule",
    )),
)
VERSION_PATTERN = r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)"


def run(args: list[str], **kwargs) -> subprocess.CompletedProcess:
    return subprocess.run(args, check=True, **kwargs)


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_version(root: Path = ROOT, tag: str | None = None) -> str:
    version = ET.parse(root / "UEI.csproj").findtext("./PropertyGroup/Version")
    source = (root / "src/Plugin.cs").read_text(encoding="utf-8-sig")
    base = re.search(r'PluginVersionBase\s*=\s*"([^"]+)"', source)
    suffix = re.search(r'PluginVersion\s*=\s*PluginVersionBase\s*\+\s*"([^"]+)"', source)
    if not version or not re.fullmatch(VERSION_PATTERN, version):
        raise ValueError("UEI.csproj Version must be x.x.x (no suffix).")
    if not base or not suffix or base[1] + suffix[1] != version:
        raise ValueError("src/Plugin.cs and UEI.csproj versions must match.")
    if tag is not None and tag != "v" + version:
        raise ValueError(f"Release tag must be v{version}; got {tag!r}.")
    return version


def read_release_notes(root: Path = ROOT) -> str:
    version = read_version(root)
    raw = (root / "packaging/RELEASE-NOTES.md").read_text(encoding="utf-8-sig")
    header, _, body = raw.partition("\n")
    if header != f"<!-- version: {version} -->" or not body.strip():
        raise ValueError(f"Write release notes for {version} and update the version marker.")
    return body.strip() + "\n"


def reference_manifest(root: Path = ROOT) -> dict:
    manifest = json.loads((root / REFERENCE_DIR / "manifest.json").read_text(encoding="utf-8"))
    if manifest.get("format") != 1 or set(manifest.get("files", {})) != set(REFERENCES):
        raise ValueError("Unexpected reference bundle manifest.")
    return manifest


def verify_references(game_root: Path, root: Path = ROOT) -> None:
    manifest = reference_manifest(root)
    for relative, entry in manifest["files"].items():
        data = (game_root / relative).read_bytes()
        if len(data) != entry["size"] or digest(data) != entry["sha256"]:
            raise ValueError(f"Build reference does not match pinned manifest: {relative}")


def gpg_file(mode: str, source: Path, target: Path, key: str, gpg: str = "gpg") -> None:
    # The key travels over stdin, never the command line or a plaintext key file.
    with tempfile.TemporaryDirectory(prefix="uei-gpg-") as home:
        os.chmod(home, 0o700)
        args = [gpg, "--no-options", "--homedir", home, "--batch", "--yes",
                "--pinentry-mode", "loopback", "--passphrase-fd", "0",
                "--no-symkey-cache", "--output", str(target)]
        if mode == "encrypt":
            args += ["--symmetric", "--cipher-algo", "AES256", "--s2k-mode", "3",
                     "--s2k-count", "65011712", str(source)]
        else:
            args += ["--decrypt", str(source)]
        run(args, input=(key + "\n").encode(), stdout=subprocess.DEVNULL)


def prepare_references(game_root: Path, gpg: str, provision: bool, root: Path = ROOT) -> None:
    key = os.environ.get(SECRET)
    if not key and not provision:
        raise ValueError(f"Set {SECRET}, or pass --provision-secret to generate/store a new key.")
    key = key or secrets.token_urlsafe(64)
    files = {relative: (game_root / relative).read_bytes() for relative in REFERENCES}
    manifest = {"format": 1, "files": {name: {"size": len(data), "sha256": digest(data)}
                                               for name, data in files.items()}}
    destination = root / REFERENCE_DIR
    destination.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="uei-reference-pack-") as temp:
        temp = Path(temp)
        plain, encrypted, restored = (temp / n for n in ("refs.zip", "refs.zip.gpg", "restored.zip"))
        with zipfile.ZipFile(plain, "w", zipfile.ZIP_DEFLATED) as archive:
            for name, data in files.items():
                archive.writestr(name, data)
        gpg_file("encrypt", plain, encrypted, key, gpg)
        gpg_file("decrypt", encrypted, restored, key, gpg)
        if restored.read_bytes() != plain.read_bytes():
            raise ValueError("Encrypted reference round-trip failed.")
        if provision:
            run(["gh", "secret", "set", SECRET, "--repo", REPOSITORY], input=key.encode())
        shutil.copyfile(encrypted, destination / "game-references.zip.gpg")
    (destination / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Prepared {len(files)} encrypted build references; plaintext/key not written to the repository.")


def decrypt_references(output: Path, root: Path = ROOT) -> None:
    key = os.environ.get(SECRET)
    if not key:
        raise ValueError(f"Repository secret {SECRET} is not configured.")
    manifest = reference_manifest(root)
    with tempfile.TemporaryDirectory(prefix="uei-reference-unpack-") as temp:
        plain = Path(temp) / "refs.zip"
        gpg_file("decrypt", root / REFERENCE_DIR / "game-references.zip.gpg", plain, key)
        with zipfile.ZipFile(plain) as archive:
            if sorted(archive.namelist()) != sorted(REFERENCES):
                raise ValueError("Reference archive has unexpected/missing/duplicate entries.")
            for name, entry in manifest["files"].items():
                if archive.getinfo(name).file_size != entry["size"]:
                    raise ValueError(f"Reference size mismatch: {name}")
                data = archive.read(name)
                if digest(data) != entry["sha256"]:
                    raise ValueError(f"Reference hash mismatch: {name}")
                target = output / name
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(data)
    verify_references(output, root)
    print(f"Verified {len(REFERENCES)} build references.")


def build(game_root: Path, output: Path, root: Path = ROOT) -> Path:
    read_version(root)
    verify_references(game_root, root)
    # A fresh temporary output prevents a stale checked-out DLL being packaged.
    with tempfile.TemporaryDirectory(prefix="uei-build-") as temp:
        temp = Path(temp)
        obj = str(temp / "obj") + os.sep
        run(["dotnet", "build", str(root / "UEI.csproj"), "-c", "Release",
             "--nologo", "-warnaserror", "-p:ContinuousIntegrationBuild=true",
             f"-p:GameRoot={game_root.resolve()}", f"-p:OutputPath={temp / 'out'}{os.sep}",
             f"-p:BaseIntermediateOutputPath={obj}", f"-p:IntermediateOutputPath={obj}",
             f"-p:MSBuildProjectExtensionsPath={obj}"], cwd=root)
        dll = temp / "out/UEI.build.dll"
        if not dll.is_file():
            raise ValueError("Build succeeded without the expected UEI.build.dll.")
        output.mkdir(parents=True, exist_ok=True)
        target = output / "UEI.dll"
        shutil.copyfile(dll, target)
    return target


def source_commit(root: Path = ROOT) -> str:
    return run(["git", "rev-parse", "HEAD"], cwd=root, capture_output=True, text=True).stdout.strip()


def package(dll: Path, output: Path, root: Path = ROOT) -> None:
    version = read_version(root)
    output.mkdir(parents=True, exist_ok=True)
    payload = dll.read_bytes()
    shutil.copyfile(dll, output / "UEI.dll")
    icon = (root / "packaging/icon.png").read_bytes()
    if icon[:8] != b"\x89PNG\r\n\x1a\n" or struct.unpack(">II", icon[16:24]) != (256, 256):
        raise ValueError("Package icon must be a 256x256 PNG.")
    manifest = {"name": "UEI", "version_number": version,
                "website_url": f"https://github.com/{REPOSITORY}",
                "description": "Item, liquid and recipe browser for Casualties Unknown.",
                "dependencies": ["BepInEx-BepInExPack-5.4.2305"]}
    entries = {"BepInEx/plugins/UEI/UEI.dll": payload, "icon.png": icon,
               "CHANGELOG.md": (f"# {version}\n\n" + read_release_notes(root)).encode("utf-8"),
               "manifest.json": (json.dumps(manifest, indent=2) + "\n").encode(),
               "README.md": (root / "packaging/README.md").read_text(encoding="utf-8").replace("@VERSION@", version).encode(),
               "LICENSE": (root / "LICENSE").read_bytes()}
    with zipfile.ZipFile(output / f"UEI-{version}.zip", "w", zipfile.ZIP_DEFLATED) as archive:
        for name, data in entries.items():
            info = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            archive.writestr(info, data)
    info = {"version": version, "source_commit": source_commit(root),
            "target_framework": "netstandard2.1",
            "references_manifest_sha256": digest((root / REFERENCE_DIR / "manifest.json").read_bytes())}
    (output / "build-info.json").write_text(json.dumps(info, indent=2) + "\n", encoding="utf-8")
    names = [f"UEI-{version}.zip", "UEI.dll", "build-info.json"]
    (output / "SHA256SUMS.txt").write_text("".join(
        digest((output / name).read_bytes()) + "  " + name + "\n" for name in names), encoding="utf-8")
    (output / "release-notes.md").write_text(read_release_notes(root), encoding="utf-8")
    verify_package(output, root)
    print(f"Created and verified UEI {version} release assets.")


def verify_package(output: Path, root: Path = ROOT) -> None:
    version = read_version(root)
    names = {f"UEI-{version}.zip", "UEI.dll", "build-info.json"}
    if {p.name for p in output.iterdir()} != names | {"SHA256SUMS.txt", "release-notes.md"}:
        raise ValueError("Release directory contains missing or unexpected files.")
    if (output / "release-notes.md").read_text(encoding="utf-8") != read_release_notes(root):
        raise ValueError("Release notes do not match the checked-out source.")
    checksums = {}
    for line in (output / "SHA256SUMS.txt").read_text(encoding="utf-8").splitlines():
        checksum, name = line.split("  ", 1)
        if name in checksums or name not in names or digest((output / name).read_bytes()) != checksum:
            raise ValueError("Invalid release checksum manifest.")
        checksums[name] = checksum
    if set(checksums) != names:
        raise ValueError("Missing release checksums.")
    info = json.loads((output / "build-info.json").read_text(encoding="utf-8"))
    if info["version"] != version or info["source_commit"] != source_commit(root):
        raise ValueError("Release provenance does not match the checked-out source.")
    with zipfile.ZipFile(output / f"UEI-{version}.zip") as archive:
        expected = ["BepInEx/plugins/UEI/UEI.dll", "CHANGELOG.md", "LICENSE", "README.md", "icon.png", "manifest.json"]
        if sorted(archive.namelist()) != expected or archive.testzip() is not None:
            raise ValueError("Release ZIP contains unexpected/missing files or is corrupt.")
        if archive.read("BepInEx/plugins/UEI/UEI.dll") != (output / "UEI.dll").read_bytes():
            raise ValueError("ZIP and standalone UEI DLL differ.")
        if archive.read("icon.png") != (root / "packaging/icon.png").read_bytes():
            raise ValueError("ZIP icon does not match the selected source icon.")
        changelog = (f"# {version}\n\n" + read_release_notes(root)).encode("utf-8")
        if archive.read("CHANGELOG.md") != changelog:
            raise ValueError("ZIP changelog does not match this version's release notes.")
        manifest = json.loads(archive.read("manifest.json"))
        if manifest["version_number"] != version or manifest["dependencies"] != ["BepInEx-BepInExPack-5.4.2305"]:
            raise ValueError("Incorrect package version/dependencies.")
        if b"@VERSION@" in archive.read("README.md"):
            raise ValueError("Unresolved README version placeholder.")


def publish(output: Path, tag: str, root: Path = ROOT) -> None:
    version = read_version(root, tag)
    verify_package(output, root)
    commit = source_commit(root)
    tag_commit = run(["git", "rev-parse", f"refs/tags/{tag}^{{commit}}"], cwd=root, capture_output=True, text=True).stdout.strip()
    if tag_commit != commit:
        raise ValueError("Release tag does not point to the checked-out build commit.")
    run(["git", "merge-base", "--is-ancestor", commit, "origin/main"], cwd=root)
    assets = [output / name for name in (f"UEI-{version}.zip", "UEI.dll", "build-info.json", "SHA256SUMS.txt")]
    # Stage privately first. A failed create/upload never reaches the publish step.
    # No --clobber: an existing release is never silently replaced.
    run(["gh", "release", "create", tag, "--repo", REPOSITORY, "--draft",
         "--verify-tag", "--title", f"UEI {version}",
         "--notes-file", str(output / "release-notes.md"), *map(str, assets)], cwd=root)
    run(["gh", "release", "edit", tag, "--repo", REPOSITORY, "--draft=false"], cwd=root)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    p = commands.add_parser("version")
    p.add_argument("--tag")
    p = commands.add_parser("prepare-references")
    p.add_argument("--game-root", type=Path, required=True)
    p.add_argument("--gpg", default="gpg")
    p.add_argument("--provision-secret", action="store_true")
    p = commands.add_parser("decrypt-references")
    p.add_argument("--output", type=Path, required=True)
    p = commands.add_parser("build")
    p.add_argument("--game-root", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    for name in ("package", "verify-package", "publish"):
        p = commands.add_parser(name)
        p.add_argument("--output", type=Path, required=True)
        if name == "package":
            p.add_argument("--dll", type=Path, required=True)
        if name == "publish":
            p.add_argument("--tag", required=True)
    args = parser.parse_args()
    if args.command == "version":
        version = read_version(tag=args.tag)
        read_release_notes()
        print(version)
        if os.environ.get("GITHUB_OUTPUT"):
            with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as stream:
                stream.write(f"version={version}\n")
    elif args.command == "prepare-references":
        prepare_references(args.game_root, args.gpg, args.provision_secret)
    elif args.command == "decrypt-references":
        decrypt_references(args.output)
    elif args.command == "build":
        build(args.game_root, args.output)
    elif args.command == "package":
        package(args.dll, args.output)
    elif args.command == "verify-package":
        verify_package(args.output)
    elif args.command == "publish":
        publish(args.output, args.tag)


if __name__ == "__main__":
    main()
