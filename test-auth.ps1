# Test Authentication and Get JWT Token
param(
    [string]$BaseUrl = "http://localhost:7071",
    [string]$Email = "test@example.com",
    [string]$Password = "TestPassword123!",
    [string]$Name = "Test User"
)

# Colors for output
$SuccessColor = "Green"
$ErrorColor = "Red"
$InfoColor = "Cyan"
$WarningColor = "Yellow"

Write-Host "=== TodoApp Authentication Test ===" -ForegroundColor $InfoColor
Write-Host

# Function to make HTTP request
function Invoke-AuthRequest {
    param($Url, $Body)
    
    try {
        $headers = @{ "Content-Type" = "application/json" }
        $response = Invoke-RestMethod -Uri $Url -Method POST -Body ($Body | ConvertTo-Json) -Headers $headers
        return @{ Success = $true; Data = $response }
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
        
        return @{ Success = $false; StatusCode = $statusCode; Error = $errorMessage }
    }
}

# Test Registration
Write-Host "1️⃣  Testing User Registration..." -ForegroundColor $InfoColor
$registerBody = @{
    email = $Email
    password = $Password
    name = $Name
}

$registerResult = Invoke-AuthRequest "$BaseUrl/api/auth/register" $registerBody

if ($registerResult.Success) {
    Write-Host "✅ Registration Successful!" -ForegroundColor $SuccessColor
    Write-Host "User ID: $($registerResult.Data.user.id)" -ForegroundColor $InfoColor
    Write-Host "Email: $($registerResult.Data.user.email)" -ForegroundColor $InfoColor
    Write-Host "Name: $($registerResult.Data.user.name)" -ForegroundColor $InfoColor
} else {
    if ($registerResult.StatusCode -eq 400 -and $registerResult.Error -like "*already exists*") {
        Write-Host "⚠️  User already exists, proceeding to login..." -ForegroundColor $WarningColor
    } else {
        Write-Host "❌ Registration Failed!" -ForegroundColor $ErrorColor
        Write-Host "Status Code: $($registerResult.StatusCode)" -ForegroundColor $ErrorColor
        Write-Host "Error: $($registerResult.Error)" -ForegroundColor $ErrorColor
        Write-Host
        exit 1
    }
}

Write-Host

# Test Login
Write-Host "2️⃣  Testing User Login..." -ForegroundColor $InfoColor
$loginBody = @{
    email = $Email
    password = $Password
}

$loginResult = Invoke-AuthRequest "$BaseUrl/api/auth/login" $loginBody

if ($loginResult.Success) {
    Write-Host "✅ Login Successful!" -ForegroundColor $SuccessColor
    Write-Host "User ID: $($loginResult.Data.user.id)" -ForegroundColor $InfoColor
    Write-Host "Email: $($loginResult.Data.user.email)" -ForegroundColor $InfoColor
    Write-Host "Token: $($loginResult.Data.token.Substring(0, 50))..." -ForegroundColor $InfoColor
    
    # Save token to file for import test
    $loginResult.Data.token | Out-File -FilePath "jwt-token.txt" -Encoding utf8
    Write-Host "💾 Token saved to jwt-token.txt" -ForegroundColor $SuccessColor
    
    Write-Host
    Write-Host "🎯 Ready for Import Test! Run:" -ForegroundColor $SuccessColor
    Write-Host ".\test-import.ps1 -Token '$($loginResult.Data.token)'" -ForegroundColor $InfoColor
    
} else {
    Write-Host "❌ Login Failed!" -ForegroundColor $ErrorColor
    Write-Host "Status Code: $($loginResult.StatusCode)" -ForegroundColor $ErrorColor
    Write-Host "Error: $($loginResult.Error)" -ForegroundColor $ErrorColor
}

Write-Host
Write-Host "=== Authentication Test Complete ===" -ForegroundColor $InfoColor