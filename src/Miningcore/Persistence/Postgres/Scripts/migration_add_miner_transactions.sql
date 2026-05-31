-- Migration: Add coin-scoped miner_transactions and indexer_state tables
-- Run this script on existing installations that were set up before this feature was added.
--
-- This script is intentionally comprehensive and idempotent:
-- 1) Creates the final coin-scoped schema when tables do not exist.
-- 2) If a local pre-release pool-scoped shape exists, it drops and recreates
--    those tables to the final coin-scoped shape.

BEGIN;

DO $$
BEGIN
	IF EXISTS (
		SELECT 1
		FROM information_schema.columns
		WHERE table_schema = 'public'
		  AND table_name = 'miner_transactions'
		  AND column_name = 'poolid')
	THEN
		DROP TABLE IF EXISTS miner_transactions CASCADE;
	END IF;

	IF EXISTS (
		SELECT 1
		FROM information_schema.columns
		WHERE table_schema = 'public'
		  AND table_name = 'indexer_state'
		  AND column_name = 'poolid')
	THEN
		DROP TABLE IF EXISTS indexer_state CASCADE;
	END IF;
END $$;

CREATE TABLE IF NOT EXISTS miner_transactions
(
	id BIGSERIAL NOT NULL PRIMARY KEY,
	coin TEXT NOT NULL,
	txid TEXT NOT NULL,
	address TEXT NOT NULL,
	amount decimal(28,12) NOT NULL,
	category TEXT NOT NULL,
	blockheight BIGINT NOT NULL,
	blockhash TEXT NOT NULL,
	blocktime TIMESTAMPTZ NOT NULL,
	created TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS IDX_MINER_TX_COIN_TXID_ADDRESS ON miner_transactions(coin, txid, address);
CREATE INDEX IF NOT EXISTS IDX_MINER_TX_COIN_ADDRESS_TIME ON miner_transactions(coin, address, blocktime DESC);
CREATE INDEX IF NOT EXISTS IDX_MINER_TX_COIN_HEIGHT ON miner_transactions(coin, blockheight);

CREATE TABLE IF NOT EXISTS indexer_state
(
	coin TEXT NOT NULL PRIMARY KEY,
	lastindexedheight BIGINT NOT NULL DEFAULT 0,
	updated TIMESTAMPTZ NOT NULL
);

COMMIT;
