# Additional logic/error review input

Owner of requirements remains the logic specialist. This file is an input to assess, not an approved implementation request.

During intake, `Parsing/ApiEnvelope.cs` was observed to check that `body["code"]` is an Integer token, then call `codeToken.Value<int>()` without range validation. Please evaluate the concrete malformed response `{"code":2147483648,"data":{}}` (and a negative value below Int32.MinValue) against the repository invariant that malformed wire input produces a typed parse failure instead of an exception. Inspect analogous action-envelope readers and any existing tests before proposing a minimal follow-up slice. Classify actual checked behavior vs source-only hypothesis; do not broaden this into unrelated parser migration or account changes.

If accepted as a concrete issue, define a narrow shared/error-boundary fix and meaningful regression behavior (out-of-range code never succeeds or throws; an in-range nonzero business code and its message remain intact; ordinary zero/empty payload behavior remains unchanged). Requirements/review gates still apply before product/test edits.
