# Issue #1216 isolated source probe

`run-terminal-comment-cursor.sh` extracts the target response-cursor and load-more statements, then executes them with a local cursor/button boundary. It checks a ten-reply terminal page and a ten-reply page with a next offset. The test does not call the comment API or run the UWP app.
