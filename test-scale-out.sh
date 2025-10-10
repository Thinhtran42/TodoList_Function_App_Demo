#!/bin/bash

# Script to test Worker scale out by sending multiple CSV import requests

API_URL="http://localhost:5231/api/import"
CSV_FILE="test-import.csv"
TOKEN="YOUR_JWT_TOKEN_HERE"  # Get token from /api/auth/login

echo "🚀 Testing Worker Scale Out with 10 parallel CSV imports..."
echo "================================================"

# Send 10 import requests in parallel
for i in {1..10}; do
  echo "📤 Sending import request #$i..."
  curl -X POST "$API_URL" \
    -H "Authorization: Bearer $TOKEN" \
    -F "file=@$CSV_FILE" \
    &
done

wait

echo ""
echo "✅ All requests sent! Check Worker logs and Hangfire Dashboard"
echo ""
echo "View logs:"
echo "  docker logs -f todolist_function_app_demo-todoapp-worker-1"
echo "  docker logs -f todolist_function_app_demo-todoapp-worker-2"
echo "  docker logs -f todolist_function_app_demo-todoapp-worker-3"
echo ""
echo "Hangfire Dashboard: http://localhost:5231/hangfire"
echo "RabbitMQ Management: http://localhost:15672"
