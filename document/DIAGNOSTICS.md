# Collecting diagnostic logs

To report a problem, note the app version, the approximate time, the steps that led to it, and any visible error text. In **Settings → 诊断**, use **标记问题** at the point you notice an issue; the optional note is written to the persistent log with the time and session ID.

**导出日志** saves a text file containing the current session ID, the app version, and the seven most recent daily log files with their source filenames. **打开日志目录** opens the folder those files live in (`LocalState\log`) in the file manager, which is what you want when you need every file rather than the last seven. If the app closed unexpectedly, relaunch it before exporting so the earlier session's log is included.

Include the exported file with your report and the time, steps, visible error, and app/build version. Logs can contain request details or other information from app activity, so review the exported text before sharing it. A native process termination can prevent managed exception handlers from running; the logs may not contain a complete stack trace for every hard crash.

Older builds had a 实时日志 switch with an in-app log box and an API parameter editor. Both are gone: the log file is written unconditionally, and the app keys and API endpoints are fixed in the build, so there is nothing to configure before collecting a log.
