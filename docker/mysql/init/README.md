Place SQL initialization files here if you want Docker to auto-import data on first startup.

Important:
- This folder is only applied when the database volume is empty.
- File order is lexicographic (01_schema.sql, 02_data.sql, ...).
- To re-run init scripts, remove the volume: docker compose down -v
