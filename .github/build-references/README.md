# Private build inputs

`game-references.zip.gpg` contains only the ten compile-time DLL references listed
in `manifest.json`, encrypted with a random key and GnuPG AES-256. The key is held
in the `UEI_BUILD_REFERENCES_KEY` repository Actions secret. The manifest records
the exact sizes and SHA-256 hashes used for compilation.

Trusted push/manual builds decrypt into `RUNNER_TEMP`, validate every reference,
and compile against these actual assemblies. Pull-request jobs do not receive
the key. Neither the references nor the key are included in public build
artifacts, release packages, or caches.

To update references after a game/BepInEx API update, use the maintainer procedure
in `docs/github-automation.md`. Do not commit plaintext game assemblies or keys.
