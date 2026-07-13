# Test Error Handling Script
# This script tests that 400 errors now show proper error messages

$baseUrl = "http://localhost:5142"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Error Handling Improvements" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Hangup without Call Connection ID (should return 400 with error message)
Write-Host "Test 1: Hangup without Call Connection ID" -ForegroundColor Yellow
Write-Host "Expected: 400 with error message 'Call Connection ID is required'" -ForegroundColor Gray
try {
	$body = @{
		callConnectionId = ""
		forEveryone = $false
	} | ConvertTo-Json

	$response = Invoke-WebRequest `
		-Uri "$baseUrl/api/v2/calls/hangup" `
		-Method POST `
		-ContentType "application/json" `
		-Body $body `
		-ErrorAction Stop

	Write-Host "Response: $($response.Content)" -ForegroundColor Green
} catch {
	$statusCode = $_.Exception.Response.StatusCode.value__
	$errorContent = $_.ErrorDetails.Message

	Write-Host "Status Code: $statusCode" -ForegroundColor $(if ($statusCode -eq 400) { "Green" } else { "Red" })
	Write-Host "Error Response: $errorContent" -ForegroundColor $(if ($errorContent -match "error") { "Green" } else { "Red" })

	if ($errorContent -match '"error"') {
		Write-Host "✅ PASS: Error message is properly formatted" -ForegroundColor Green
	} else {
		Write-Host "❌ FAIL: Error message format incorrect" -ForegroundColor Red
	}
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

# Test 2: Create call with missing target (should return 400 with error message)
Write-Host "Test 2: Create call without target" -ForegroundColor Yellow
Write-Host "Expected: 400 with validation error (ProblemDetails format)" -ForegroundColor Gray
try {
	$body = @{
		target = ""
		sourceDisplayName = "Test"
	} | ConvertTo-Json

	$response = Invoke-WebRequest `
		-Uri "$baseUrl/api/v2/calls/create" `
		-Method POST `
		-ContentType "application/json" `
		-Body $body `
		-ErrorAction Stop

	Write-Host "Response: $($response.Content)" -ForegroundColor Green
} catch {
	$statusCode = $_.Exception.Response.StatusCode.value__
	$errorContent = $_.ErrorDetails.Message

	Write-Host "Status Code: $statusCode" -ForegroundColor $(if ($statusCode -eq 400) { "Green" } else { "Red" })
	Write-Host "Error Response: $errorContent" -ForegroundColor Green

	# Check if it's ProblemDetails format (which our frontend now supports)
	if ($errorContent -match '"title"' -or $errorContent -match '"errors"' -or $errorContent -match '"error"') {
		Write-Host "✅ PASS: Error is in a supported format (ProblemDetails or simple error)" -ForegroundColor Green
		Write-Host "   Frontend will extract: title + errors details" -ForegroundColor Gray
	} else {
		Write-Host "❌ FAIL: Error message format not recognized" -ForegroundColor Red
	}
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

# Test 3: Get configuration (should succeed)
Write-Host "Test 3: Get configuration (success case)" -ForegroundColor Yellow
Write-Host "Expected: 200 with configuration data" -ForegroundColor Gray
try {
	$response = Invoke-WebRequest `
		-Uri "$baseUrl/api/v2/configuration" `
		-Method GET `
		-ErrorAction Stop

	$statusCode = $response.StatusCode
	Write-Host "Status Code: $statusCode" -ForegroundColor $(if ($statusCode -eq 200) { "Green" } else { "Red" })
	Write-Host "Response: $($response.Content)" -ForegroundColor Green
	Write-Host "✅ PASS: Configuration retrieved successfully" -ForegroundColor Green
} catch {
	Write-Host "❌ FAIL: Configuration endpoint failed" -ForegroundColor Red
	Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Error Handling Test Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Open http://localhost:5142/test.html in your browser" -ForegroundColor White
Write-Host "2. Open browser Developer Tools (F12)" -ForegroundColor White
Write-Host "3. Go to the Console tab" -ForegroundColor White
Write-Host "4. Try the 'Hangup Call' test without entering a Call Connection ID" -ForegroundColor White
Write-Host "5. Verify that:" -ForegroundColor White
Write-Host "   - Console shows detailed 'API Error Details' and 'API Call Failed' logs" -ForegroundColor Gray
Write-Host "   - UI shows '❌ Error' header with the actual error message" -ForegroundColor Gray
Write-Host "   - Error panel is styled with red background" -ForegroundColor Gray
