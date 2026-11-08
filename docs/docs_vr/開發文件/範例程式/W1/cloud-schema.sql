CREATE TABLE IF NOT EXISTS results (
  result_id text PRIMARY KEY,
  session_id text UNIQUE NOT NULL,
  token text UNIQUE NOT NULL,
  payload jsonb NOT NULL,
  payload_hash text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);
