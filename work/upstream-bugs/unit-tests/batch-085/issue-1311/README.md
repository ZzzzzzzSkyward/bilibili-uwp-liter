# Issue 1311: preserve subtitle language in labels

Run `bash work/upstream-bugs/unit-tests/batch-085/issue-1311/run-subtitle-language-label.sh` from the repository root. It extracts the actual API menu label, download label, and AI auto-selection condition from target source and executes those statements with small local boundary stubs.

The pinned sibling displays `lan_doc(lan)` for online subtitles and stores that combined label for downloads. The target had dropped `lan` in both places, even though its existing AI check still searched labels for `自动`, `AI`, or `ai`. The probe checks that a plain display title with the existing `ai` marker in its language is skipped when auto-open is disabled, a manual language remains selected, and enabling the option allows the marked subtitle.

This restores the source-backed label information available for online subtitles and newly created downloads. Previously saved local download labels cannot regain language values that were discarded when saved, and no source-grounded way to classify fully unmarked AI tracks was found. This probe does not exercise subtitle rendering, a live API, or app runtime.
