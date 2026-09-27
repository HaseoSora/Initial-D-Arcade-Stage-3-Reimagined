-- Widen course directions to 0..31. Copy dependent replay tables before
-- replacing runs so ON DELETE CASCADE cannot remove existing recordings.
CREATE TABLE runs_tsubaki (
 id TEXT PRIMARY KEY, device_id TEXT NOT NULL REFERENCES devices(id),
 ruleset TEXT NOT NULL, epoch INTEGER NOT NULL, condition INTEGER NOT NULL CHECK(condition BETWEEN 0 AND 31),
 weather INTEGER NOT NULL CHECK(weather IN (0,1)), car INTEGER NOT NULL CHECK(car BETWEEN 0 AND 34),
 ticks INTEGER NOT NULL CHECK(ticks BETWEEN 60000 AND 10799999),
 glyphs TEXT NOT NULL, splits TEXT NOT NULL, manual INTEGER NOT NULL, night INTEGER NOT NULL,
 points INTEGER NOT NULL, build TEXT NOT NULL, created_at INTEGER NOT NULL,
 hidden INTEGER NOT NULL DEFAULT 0 CHECK(hidden IN (0,1)), reason TEXT NOT NULL DEFAULT '',
 imported INTEGER NOT NULL DEFAULT 0 CHECK(imported IN (0,1)),
 replay_size INTEGER NOT NULL DEFAULT 0, replay_sha256 TEXT NOT NULL DEFAULT ''
);
INSERT INTO runs_tsubaki SELECT * FROM runs;
CREATE TABLE replays_tsubaki (
 run_id TEXT PRIMARY KEY REFERENCES runs_tsubaki(id),
 data BLOB NOT NULL CHECK(length(data) BETWEEN 44 AND 1010000)
);
INSERT INTO replays_tsubaki SELECT * FROM replays;
CREATE TABLE replay_chunks_tsubaki (
 run_id TEXT NOT NULL REFERENCES runs_tsubaki(id) ON DELETE CASCADE,
 part INTEGER NOT NULL CHECK(part >= 0), data BLOB NOT NULL,
 PRIMARY KEY(run_id, part)
);
INSERT INTO replay_chunks_tsubaki SELECT * FROM replay_chunks;
DROP TABLE replay_chunks;
DROP TABLE replays;
DROP TABLE runs;
ALTER TABLE runs_tsubaki RENAME TO runs;
ALTER TABLE replays_tsubaki RENAME TO replays;
ALTER TABLE replay_chunks_tsubaki RENAME TO replay_chunks;
CREATE INDEX runs_board ON runs(ruleset,epoch,condition,weather,hidden,ticks,created_at);
CREATE INDEX runs_device ON runs(device_id,created_at);
CREATE UNIQUE INDEX runs_import_once ON runs(device_id,condition,weather,car,ticks) WHERE imported=1;
