-- Synthetic test data for CyberList.
--
-- Adds a mix of nested folders and tasks to the Items table WITHOUT touching existing rows:
-- new Ids start after the current highest Id. Run it against the DEV database:
--   %LOCALAPPDATA%\CyberList-Dev\cyberlist-dev.db
-- e.g. DB Browser for SQLite → Execute SQL → open this file → Run, then Write Changes.
-- Close CyberList first: it loads the list once at startup.
--
-- What it covers:
--   * folders 3–4 levels deep (tree guide lines, +/- nodes, [done/total] counters)
--   * a fully done folder (shows under 03 DONE), a partly done one (indeterminate checkbox)
--   * loose root tasks, done and open
--   * a 200-character title (the max length), unicode/emoji, an empty folder-to-be
--   * a BULK folder with 60 tasks, for scrolling
--
-- To undo: delete the roots it created (children go with them via ON DELETE CASCADE, which
-- needs foreign keys on: DB Browser has them on by default, Preferences → Database):
--   DELETE FROM Items WHERE ParentId IS NULL AND Title LIKE '[SEED]%';

-- No BEGIN/COMMIT here: DB Browser already runs Execute SQL inside its own transaction
-- ("Write Changes" commits it, "Revert Changes" throws it away), and SQLite can't nest them.

-- Every seeded Id is offset by the current max Id, so nothing collides with real rows.
CREATE TEMP TABLE seed_base AS SELECT IFNULL(MAX(Id), 0) AS base FROM Items;

-- n = local id (also the creation order, which is the display order)
-- p = local id of the parent, NULL for a root
-- A folder's IsDone is 1 only when all its tasks are done, the same rule the app uses.
CREATE TEMP TABLE seed (n INTEGER PRIMARY KEY, p INTEGER, title TEXT NOT NULL, done INTEGER NOT NULL);

INSERT INTO seed (n, p, title, done) VALUES
    -- A project, 4 levels deep, partly done
    ( 1, NULL, '[SEED] Release v1.0', 0),
    ( 2,    1, 'Build', 0),
    ( 3,    2, 'Fix CI cache', 1),
    ( 4,    2, 'Sign the Windows binaries', 0),
    ( 5,    2, 'macOS', 0),
    ( 6,    5, 'Notarize the app', 0),
    ( 7,    5, 'Test on arm64', 1),
    ( 8,    1, 'Docs', 1),
    ( 9,    8, 'Update README', 1),
    (10,    8, 'Write the changelog', 1),
    (11,    1, 'Announce on Discord', 0),

    -- Everything done: only visible under 03 DONE / 01 ALL
    (20, NULL, '[SEED] Groceries', 1),
    (21,   20, 'Milk', 1),
    (22,   20, 'Bread', 1),
    (23,   20, 'Coffee beans', 1),

    -- Nothing done, two folders side by side (sibling guide lines)
    (30, NULL, '[SEED] Apartment', 0),
    (31,   30, 'Kitchen', 0),
    (32,   31, 'Fix the tap', 0),
    (33,   31, 'Replace the light bulb', 0),
    (34,   30, 'Bedroom', 0),
    (35,   34, 'Assemble the wardrobe', 0),
    (36,   35, 'Find the missing screws', 0),
    (37,   35, 'Watch the assembly video', 0),
    (38,   34, 'Paint the wall', 0),

    -- Loose root tasks
    (40, NULL, '[SEED] Call the dentist', 0),
    (41, NULL, '[SEED] Pay the internet bill', 1),
    (42, NULL, '[SEED] Ünïcödé, emoji 🚀 and symbols // <> & "quotes"', 0),
    -- Exactly 200 characters: the column's max length
    (43, NULL, '[SEED] ' || substr(
        'A very long task title that keeps going to check wrapping and clipping in the row layout. '
     || 'A very long task title that keeps going to check wrapping and clipping in the row layout. '
     || 'A very long task title that keeps going.', 1, 193), 0),

    -- Container for the bulk rows below
    (100, NULL, '[SEED] BULK (60 tasks)', 0);

-- 60 generated tasks under BULK, every third one done
WITH RECURSIVE counter(i) AS (SELECT 1 UNION ALL SELECT i + 1 FROM counter WHERE i < 60)
INSERT INTO seed (n, p, title, done)
SELECT 100 + i, 100, printf('Bulk task %02d', i), CASE WHEN i % 3 = 0 THEN 1 ELSE 0 END
FROM counter;

-- Write the real rows. CreatedAt is UTC (like DateTime.UtcNow) in EF Core's text format, one
-- second apart in n order, so parents always come before their children.
INSERT INTO Items (Id, ParentId, Title, IsDone, CreatedAt)
SELECT b.base + s.n,
       CASE WHEN s.p IS NULL THEN NULL ELSE b.base + s.p END,
       s.title,
       s.done,
       strftime('%Y-%m-%d %H:%M:%f', 'now', printf('-%d seconds', 1000 - s.n))
FROM seed s, seed_base b
ORDER BY s.n;

DROP TABLE seed;
DROP TABLE seed_base;
