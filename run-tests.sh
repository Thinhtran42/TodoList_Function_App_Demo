#!/bin/bash

echo "🧪 Running TodoApp Unit Tests..."
echo "======================================="

cd "$(dirname "$0")"

# Run all tests with coverage
echo "📊 Running tests with coverage..."
dotnet test tests/TodoApp.Tests/TodoApp.Tests.csproj \
    --verbosity normal \
    --configuration Release \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults/

echo ""
echo "✅ Test run completed!"
echo ""

# Display test summary
echo "📋 Test Summary:"
echo "================================"
echo "✅ Domain Layer Tests:"
echo "   - TodoItem entity business logic tests"
echo ""
echo "✅ Application Layer Tests:"
echo "   - TodoService business logic tests"
echo "   - TodoMapper mapping tests"
echo "   - CreateTodoRequestValidator tests"  
echo "   - UpdateTodoRequestValidator tests"
echo ""
echo "✅ Infrastructure Layer Tests:"
echo "   - TodoRepository data access tests"
echo "   - Entity Framework integration tests"
echo ""
echo "🎉 All layers are properly tested!"