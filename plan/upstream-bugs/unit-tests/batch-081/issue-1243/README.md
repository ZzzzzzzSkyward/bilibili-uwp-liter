# Empty note-card URL

Run `bash plan/upstream-bugs/unit-tests/batch-081/issue-1243/run-empty-dynamic-url.sh` from the repository root with the existing isolated-unit tooling installed. The runner extracts the actual `HandleUrl` prefix by method and comment anchors. It checks that an empty string returns `false`, the existing `@` case still returns `false`, and an ordinary URL passes through the prefix.

Recorded result:

```text
Empty URL returns false: True
Existing @ prefix returns false: True
Ordinary URL continues: True
PASS: empty URL is rejected and existing prefix behavior is preserved
```

The runner emits CS1998 because the extracted async prefix has no await. This probe executes only the extracted handler prefix; it does not exercise URL navigation, the UI, or a live service. No application build or restore is involved.
