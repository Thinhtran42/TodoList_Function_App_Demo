# Test Import CSV Endpoint
param(
    [string]$BaseUrl = "http://localhost:7071",
    [string]$CsvFile = "test-import.csv",
    [string]$Token = ""
)

# Colors for output
$SuccessColor = "Green"
$ErrorColor = "Red"
$InfoColor = "Cyan"
$WarningColor = "Yellow"

Write-Host "=== TodoApp CSV Import Test ===" -ForegroundColor $InfoColor
Write-Host

# Check if CSV file exists
if (-not (Test-Path $CsvFile)) {
    Write-Host "❌ CSV file '$CsvFile' not found!" -ForegroundColor $ErrorColor
    exit 1
}

# Read CSV content
$csvContent = Get-Content $CsvFile -Raw
Write-Host "📁 CSV Content:" -ForegroundColor $InfoColor
Write-Host $csvContent
Write-Host

# Check if token is provided
if ([string]::IsNullOrEmpty($Token)) {
    Write-Host "⚠️  No JWT token provided. You'll need to:" -ForegroundColor $WarningColor
    Write-Host "   1. Register/Login to get a JWT token" -ForegroundColor $WarningColor
    Write-Host "   2. Run: .\test-import.ps1 -Token 'your-jwt-token'" -ForegroundColor $WarningColor
    Write-Host
    Write-Host "🔑 To get token, first register/login:" -ForegroundColor $InfoColor
    Write-Host "POST $BaseUrl/api/auth/register" -ForegroundColor $InfoColor
    Write-Host "POST $BaseUrl/api/auth/login" -ForegroundColor $InfoColor
    Write-Host
    exit 1
}

# Prepare headers
$headers = @{
    "Authorization" = "Bearer $Token"
    "Content-Type" = "text/plain"
}

# Import endpoint
$importUrl = "$BaseUrl/api/todos/import"

Write-Host "🚀 Testing Import Endpoint: $importUrl" -ForegroundColor $InfoColor
Write-Host

try {
    # Send import request
    $response = Invoke-RestMethod -Uri $importUrl -Method POST -Body $csvContent -Headers $headers
    
    Write-Host "✅ Import Request Successful!" -ForegroundColor $SuccessColor
    Write-Host
    Write-Host "📊 Import Results:" -ForegroundColor $InfoColor
    Write-Host "Status: $($response.Status)" -ForegroundColor $(if ($response.Status -eq "Completed") { $SuccessColor } else { $WarningColor })
    Write-Host "Total Records: $($response.TotalRecords)" -ForegroundColor $InfoColor
    Write-Host "Imported Records: $($response.ImportedRecords)" -ForegroundColor $SuccessColor
    Write-Host "Failed Records: $($response.FailedRecords)" -ForegroundColor $(if ($response.FailedRecords -gt 0) { $ErrorColor } else { $SuccessColor })
    
    if ($response.ValidationErrors -and $response.ValidationErrors.Count -gt 0) {
        Write-Host
        Write-Host "⚠️  Validation Errors:" -ForegroundColor $WarningColor
        foreach ($error in $response.ValidationErrors) {
            Write-Host "   Line $($error.LineNumber): $($error.Errors -join ', ')" -ForegroundColor $ErrorColor
        }
    }
    
    if ($response.ProcessingErrors -and $response.ProcessingErrors.Count -gt 0) {
        Write-Host
        Write-Host "❌ Processing Errors:" -ForegroundColor $ErrorColor
        foreach ($error in $response.ProcessingErrors) {
            Write-Host "   $error" -ForegroundColor $ErrorColor
        }
    }
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
    $errorBody = ""
    
    try {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $errorBody = $reader.ReadToEnd()
        $errorJson = $errorBody | ConvertFrom-Json
        $errorMessage = $errorJson.error
    } catch {
        $errorMessage = $_.Exception.Message
    }
    
    Write-Host "❌ Import Request Failed!" -ForegroundColor $ErrorColor
    Write-Host "Status Code: $statusCode" -ForegroundColor $ErrorColor
    Write-Host "Error: $errorMessage" -ForegroundColor $ErrorColor
    
    if ($statusCode -eq 401) {
        Write-Host
        Write-Host "🔑 Token might be expired or invalid. Try getting a new token:" -ForegroundColor $WarningColor
        Write-Host "POST $BaseUrl/api/auth/login" -ForegroundColor $InfoColor
    }
}

Write-Host
Write-Host "=== Test Complete ===" -ForegroundColor $InfoColor