CREATE SCHEMA IF NOT EXISTS app;

-- Drop existing tables if they exist (in correct order due to foreign keys)
DROP TABLE IF EXISTS app.refresh_tokens;
DROP TABLE IF EXISTS app.todo_items;
DROP TABLE IF EXISTS app.users;

-- Create the users table
CREATE TABLE app.users (
    id BIGSERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create the refresh_tokens table
CREATE TABLE app.refresh_tokens (
    id BIGSERIAL PRIMARY KEY,
    token VARCHAR(255) NOT NULL UNIQUE,
    user_id BIGINT NOT NULL REFERENCES app.users(id) ON DELETE CASCADE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create the todo_items table with user_id foreign key
CREATE TABLE app.todo_items (
    id BIGSERIAL PRIMARY KEY,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    priority INTEGER NOT NULL DEFAULT 2, -- 1=Low, 2=Medium, 3=High, 4=Critical
    category INTEGER NOT NULL DEFAULT 1, -- 1=General, 2=Work, 3=Personal, etc.
    due_date TIMESTAMP WITH TIME ZONE,
    tags VARCHAR(500),
    user_id BIGINT NOT NULL REFERENCES app.users(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create indexes for better performance
-- Users table indexes
CREATE INDEX idx_users_username ON app.users(username);
CREATE INDEX idx_users_email ON app.users(email);
CREATE INDEX idx_users_is_active ON app.users(is_active);

-- Refresh tokens table indexes
CREATE INDEX idx_refresh_tokens_user_id ON app.refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token ON app.refresh_tokens(token);
CREATE INDEX idx_refresh_tokens_expires_at ON app.refresh_tokens(expires_at);

-- Todo items table indexes
CREATE INDEX idx_todo_items_user_id ON app.todo_items(user_id);
CREATE INDEX idx_todo_items_is_completed ON app.todo_items(is_completed);
CREATE INDEX idx_todo_items_priority ON app.todo_items(priority);
CREATE INDEX idx_todo_items_category ON app.todo_items(category);
CREATE INDEX idx_todo_items_due_date ON app.todo_items(due_date);
CREATE INDEX idx_todo_items_created_at ON app.todo_items(created_at);

-- Create trigger function for updating timestamps
CREATE OR REPLACE FUNCTION app.set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create triggers for updating timestamps
CREATE TRIGGER trg_users_updated_at
  BEFORE UPDATE ON app.users
  FOR EACH ROW EXECUTE FUNCTION app.set_updated_at();

CREATE TRIGGER trg_refresh_tokens_updated_at
  BEFORE UPDATE ON app.refresh_tokens
  FOR EACH ROW EXECUTE FUNCTION app.set_updated_at();

CREATE TRIGGER trg_todo_items_updated_at
  BEFORE UPDATE ON app.todo_items
  FOR EACH ROW EXECUTE FUNCTION app.set_updated_at();

-- Insert sample user (password is "password123")
INSERT INTO app.users (username, email, password_hash, first_name, last_name) VALUES
('testuser', 'test@example.com', '$2a$11$YQiQqK1vQZrQJ4fL5mfPLOJYTrXOk5o4G1xS8Q2ZVFjr6oQXHzXQi', 'Test', 'User'),
('admin', 'admin@example.com', '$2a$11$YQiQqK1vQZrQJ4fL5mfPLOJYTrXOk5o4G1xS8Q2ZVFjr6oQXHzXQi', 'Admin', 'User');

-- Insert sample data for the test user (user_id = 1)
INSERT INTO app.todo_items (title, description, is_completed, priority, category, due_date, tags, user_id) VALUES
('Complete project documentation', 'Write comprehensive documentation for the TodoApp project', false, 3, 2, '2025-10-01 10:00:00+00', 'work,documentation,project', 1),
('Buy groceries', 'Milk, Bread, Eggs, Fruits', false, 2, 3, '2025-09-30 18:00:00+00', 'personal,shopping', 1),
('Schedule doctor appointment', 'Annual health checkup', false, 2, 4, '2025-10-15 14:00:00+00', 'health,appointment', 1),
('Learn new programming language', 'Research and start learning Rust programming', false, 1, 6, null, 'education,programming', 1),
('Plan vacation', 'Research destinations and book flights', false, 1, 8, '2025-11-01 12:00:00+00', 'personal,travel,vacation', 1),
('Fix website bug', 'Resolve the login issue reported by users', true, 4, 2, '2025-09-25 16:00:00+00', 'work,bug,urgent', 1);

-- Insert sample data for admin user (user_id = 2)
INSERT INTO app.todo_items (title, description, is_completed, priority, category, due_date, tags, user_id) VALUES
('Review user feedback', 'Go through user feedback and prioritize improvements', false, 2, 2, '2025-10-02 09:00:00+00', 'work,review,feedback', 2),
('Update system documentation', 'Update all system documentation for new features', false, 3, 2, '2025-10-05 14:00:00+00', 'work,documentation,system', 2);