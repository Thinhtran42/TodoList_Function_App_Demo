-- ============================================
-- TodoApp Database Initialization Script
-- ============================================
-- This script creates:
-- 1. Application schema
-- 2. Users table with authentication
-- 3. Refresh tokens table
-- 4. Todo items table with user relationship
-- 5. Indexes for performance
-- 6. Triggers for automatic timestamp updates
-- 7. Sample data for testing

CREATE SCHEMA IF NOT EXISTS app;

-- Drop existing tables if they exist (in correct order due to foreign keys)
DROP TABLE IF EXISTS app.refresh_tokens;
DROP TABLE IF EXISTS app.todo_items;
DROP TABLE IF EXISTS app.users;

-- ============================================
-- USERS TABLE
-- ============================================
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

-- ============================================
-- REFRESH TOKENS TABLE
-- ============================================
CREATE TABLE app.refresh_tokens (
    id BIGSERIAL PRIMARY KEY,
    token VARCHAR(255) NOT NULL UNIQUE,
    user_id BIGINT NOT NULL REFERENCES app.users(id) ON DELETE CASCADE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ============================================
-- TODO ITEMS TABLE
-- ============================================
CREATE TABLE app.todo_items (
    id BIGSERIAL PRIMARY KEY,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    priority INTEGER NOT NULL DEFAULT 2, -- 1=Low, 2=Medium, 3=High, 4=Critical
    category INTEGER NOT NULL DEFAULT 1, -- 1=General, 2=Work, 3=Personal, 4=Health, 5=Finance, 6=Education, 7=Shopping, 8=Travel
    due_date TIMESTAMP WITH TIME ZONE,
    tags VARCHAR(500),
    user_id BIGINT NOT NULL REFERENCES app.users(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- ============================================
-- INDEXES
-- ============================================

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

-- ============================================
-- TRIGGERS
-- ============================================

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

-- ============================================
-- SAMPLE DATA
-- ============================================

-- Insert sample users
-- Password for both users: "password123"
-- Hash generated using BCrypt with cost factor 11
INSERT INTO app.users (username, email, password_hash, first_name, last_name) VALUES
('testuser', 'test@example.com', '$2a$11$YQiQqK1vQZrQJ4fL5mfPLOJYTrXOk5o4G1xS8Q2ZVFjr6oQXHzXQi', 'Test', 'User'),
('admin', 'admin@example.com', '$2a$11$YQiQqK1vQZrQJ4fL5mfPLOJYTrXOk5o4G1xS8Q2ZVFjr6oQXHzXQi', 'Admin', 'User');

-- Insert sample todos for testuser (user_id = 1)
INSERT INTO app.todo_items (title, description, is_completed, priority, category, due_date, tags, user_id) VALUES
('Complete project documentation', 'Write comprehensive documentation for the TodoApp project', false, 3, 2, '2025-10-01 10:00:00+00', 'work,documentation,project', 1),
('Buy groceries', 'Milk, Bread, Eggs, Fruits', false, 2, 3, '2025-09-30 18:00:00+00', 'personal,shopping', 1),
('Schedule doctor appointment', 'Annual health checkup', false, 2, 4, '2025-10-15 14:00:00+00', 'health,appointment', 1),
('Learn new programming language', 'Research and start learning Rust programming', false, 1, 6, null, 'education,programming', 1),
('Plan vacation', 'Research destinations and book flights', false, 1, 8, '2025-11-01 12:00:00+00', 'personal,travel,vacation', 1),
('Fix website bug', 'Resolve the login issue reported by users', true, 4, 2, '2025-09-25 16:00:00+00', 'work,bug,urgent', 1);

-- Insert sample todos for admin (user_id = 2)
INSERT INTO app.todo_items (title, description, is_completed, priority, category, due_date, tags, user_id) VALUES
('Review user feedback', 'Go through user feedback and prioritize improvements', false, 2, 2, '2025-10-02 09:00:00+00', 'work,review,feedback', 2),
('Update system documentation', 'Update all system documentation for new features', false, 3, 2, '2025-10-05 14:00:00+00', 'work,documentation,system', 2);

-- ============================================
-- VERIFICATION
-- ============================================
-- Verify the setup
DO $$
DECLARE
  user_count INTEGER;
  todo_count INTEGER;
BEGIN
  SELECT COUNT(*) INTO user_count FROM app.users;
  SELECT COUNT(*) INTO todo_count FROM app.todo_items;
  
  RAISE NOTICE '✅ Database initialization completed!';
  RAISE NOTICE '   - Users created: %', user_count;
  RAISE NOTICE '   - Todos created: %', todo_count;
  RAISE NOTICE '';
  RAISE NOTICE 'Test credentials:';
  RAISE NOTICE '   Username: testuser or admin';
  RAISE NOTICE '   Password: password123';
END $$;
