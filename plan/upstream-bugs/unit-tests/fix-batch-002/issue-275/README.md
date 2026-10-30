# Issue 275 pinned reply insertion probe

From the repository root, run:

```sh
plan/upstream-bugs/unit-tests/fix-batch-002/issue-275/run.sh
```

The runner extracts lines 201–225 verbatim from `src/BiliLite.UWP/Controls/CommentControl.xaml.cs` into `source-fragment.csx` and executes that block twice through the provisioned `dotnet-script` runner. It asserts that the pinned model is first and marked visible, the original first normal reply and its nested child remain the same objects, duplicate reply IDs are omitted, and a pinned-only result is inserted.

The harness substitutes only the collection item source, visibility enum, and minimal comment/data holders. It does not execute `CommentControl`, UWP list controls, the full `CommentModel`, or JSON deserialization. It establishes the extracted insertion behavior for the stated fixtures, not full application behavior.

Recorded result (exit status 0):

```text
PASS: pin first; normal reply zero and nested model preserved; duplicate IDs suppressed; pinned-only insertion works
```
