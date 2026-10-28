# Empty note-card URL

Run `bash work/upstream-bugs/unit-tests/batch-081/issue-1243/run-empty-dynamic-url.sh` from the repository root with the existing isolated-unit tooling installed. The runner extracts the target prefix that calls `url.First()` and executes it with an empty string.

Recorded result:

```text
Empty URL threw: True (InvalidOperationException)
FAIL: empty URL guard throws before the unsupported-link fallback
```

Exit status 1 is the expected failing regression assertion on current source. The runner also emits CS1998 because the extracted async prefix has no await. This probe does not exercise the full URL handler, UI navigation, or a live service. No application build or restore is involved.
