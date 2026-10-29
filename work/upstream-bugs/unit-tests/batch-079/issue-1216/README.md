# Issue #1216 isolated source probe

`run-terminal-comment-cursor.sh` extracts the target API-branch flag, response-cursor, and load-more statements, then executes them with a local cursor/button boundary. It checks a modern ten-reply terminal page, a modern page with a next offset, and a legacy ten-reply page without a cursor. Legacy page-number pagination stays enabled while a terminal modern cursor hides load-more. The test does not call the comment API or run the UWP app.
