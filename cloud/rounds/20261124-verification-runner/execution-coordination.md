# Verification coordination

After the currently running final-source full suite and controls finish, do not repeat the complete suite solely because of a commit, execution receipt or other metadata change when the executed source/test/runner bytes are unchanged. Bind the passing reports to their observed SHA and input hashes, verify equivalence for later metadata-only commits, and continue to publication/actual hosted CI. New runner/source changes, failures or unresolved concerns justify focused or complete reruns as appropriate. Preserve all genuine failure evidence; do not interrupt/hide an active required gate to obtain a cleaner status.

Hosted CI remains an independent environment result and must actually run or carry an evidenced external blocker. This note changes no acceptance, ownership, action pins or test assertions.
