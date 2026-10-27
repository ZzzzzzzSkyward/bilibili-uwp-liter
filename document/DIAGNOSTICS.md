# Collecting diagnostic logs

To report a problem, note the app version, the approximate time, the steps that led to it, and any visible error text. In **Settings → Debug**, use **Mark problem** at the point you notice an issue; the optional note is written to the persistent log with the time and session ID.

Use **Export diagnostic logs** to save a text file. It contains the current session ID and app version, plus the seven most recent daily log files with their source filenames. If the app closed unexpectedly, relaunch it before exporting so the earlier session's log is included. You can also still open the log folder from the existing help link.

Include the exported file with your report and the time, steps, visible error, and app/build version. Logs can contain request details or other information from app activity, so review the exported text before sharing it. A native process termination can prevent managed exception handlers from running; the logs may not contain a complete stack trace for every hard crash.
