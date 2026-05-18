ALTER TABLE poolstats ADD COLUMN connectedworkers INT NOT NULL DEFAULT 0;
UPDATE poolstats SET connectedworkers = connectedminers;