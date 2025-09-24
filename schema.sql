CREATE SCHEMA IF NOT EXISTS app;

CREATE TABLE IF NOT EXISTS app.todo_items (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title TEXT NOT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE OR REPLACE FUNCTION app.set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_trigger t
    JOIN pg_class c ON c.oid = t.tgrelid
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE t.tgname = 'trg_todo_items_updated_at'
      AND c.relname = 'todo_items'
      AND n.nspname = 'app'
  ) THEN
    CREATE TRIGGER trg_todo_items_updated_at
    BEFORE UPDATE ON app.todo_items
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_at();
  END IF;
END$$;

INSERT INTO app.todo_items (title) VALUES ('First task') ON CONFLICT DO NOTHING;
