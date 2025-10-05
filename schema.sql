CREATE SCHEMA IF NOT EXISTS app;

-- Drop existing table if it exists
DROP TABLE IF EXISTS app.todo_items;

-- Create the todo_items table with additional fields
CREATE TABLE app.todo_items (
    id BIGSERIAL PRIMARY KEY,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    priority INTEGER NOT NULL DEFAULT 2, -- 1=Low, 2=Medium, 3=High, 4=Critical
    category INTEGER NOT NULL DEFAULT 1, -- 1=General, 2=Work, 3=Personal, etc.
    due_date TIMESTAMP WITH TIME ZONE,
    tags VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create indexes for better performance
CREATE INDEX idx_todo_items_is_completed ON app.todo_items(is_completed);
CREATE INDEX idx_todo_items_priority ON app.todo_items(priority);
CREATE INDEX idx_todo_items_category ON app.todo_items(category);
CREATE INDEX idx_todo_items_due_date ON app.todo_items(due_date);
CREATE INDEX idx_todo_items_created_at ON app.todo_items(created_at);

-- Insert sample data
INSERT INTO app.todo_items (title, description, is_completed, priority, category, due_date, tags) VALUES
('Complete project documentation', 'Write comprehensive documentation for the TodoApp project', false, 3, 2, '2025-10-01 10:00:00+00', 'work,documentation,project'),
('Buy groceries', 'Milk, Bread, Eggs, Fruits', false, 2, 3, '2025-09-30 18:00:00+00', 'personal,shopping'),
('Schedule doctor appointment', 'Annual health checkup', false, 2, 4, '2025-10-15 14:00:00+00', 'health,appointment'),
('Learn new programming language', 'Research and start learning Rust programming', false, 1, 6, null, 'education,programming'),
('Plan vacation', 'Research destinations and book flights', false, 1, 8, '2025-11-01 12:00:00+00', 'personal,travel,vacation'),
('Fix website bug', 'Resolve the login issue reported by users', true, 4, 2, '2025-09-25 16:00:00+00', 'work,bug,urgent');

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
