# Read-only release-signing inventory

This is a documentation/release-prerequisite finding, not a signing failure reproduction or permission to change signing material.

- `file src/BiliLite.UWP/BiliLite.UWP.cer` identifies ASCII/PEM certificate text; the initial DER-specific reader rejected that format, then the PEM reader succeeded.
- `openssl x509 -in src/BiliLite.UWP/BiliLite.UWP.cer -noout -dates`: notBefore May 17 2023 16:27:48 GMT; **notAfter May 16 2024 22:27:48 GMT**.
- `openssl x509 -in ... -noout -fingerprint -sha1`: public certificate fingerprint **42621A9CE0525C9351F5142E87F349D063DFE89B**.
- Both UWP csproj and Packages wapproj enable signing and select **3F95D7B0B185635BA827AF4A3E82E31EC3675FCF**. This **differs** from the tracked CER fingerprint. The UWP PackageCertificateKeyFile property is empty; this Linux checkout has no inspected Windows signing store/private key.

Therefore the tracked CER is an expired, different certificate; it does **not** establish that the currently selected Windows signing certificate is expired or that a real current package fails signing. Active signing-store validity/key availability/publisher match remain **BLOCKED — Windows release environment unavailable**. Do not change CER, thumbprints, manifests, identity, keys or repository/account settings. Only a maintainer's future authorized release preparation can reconcile distribution certificate material with the actual selected/installed signer.

Documentation owner input: remove or qualify README's unsupported volatile “signature valid through 2026” sentence rather than infer an active certificate expiry. Add a stable before-release check in BUILD/DEVELOPMENT: inspect the actual selected Windows-store certificate's validity/private-key/publisher against the package and supplied install certificate. Keep historical artifact data and present uncertainty explicit in ROADMAP/canonical maintenance references, not as round memory in AGENTS/README. No additional product/test/build change or unit test is requested.
