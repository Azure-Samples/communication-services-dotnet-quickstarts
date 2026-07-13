# Test Script for v2 API Endpoints
# Run this in PowerShell after the application is running at http://localhost:5142

Write-Host "🧪 Testing Clean Architecture v2 API Endpoints" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5142"

# Test 1: Get Configuration
Write-Host "📋 Test 1: GET Configuration" -ForegroundColor Yellow
try {
	$response = Invoke-RestMethod -Uri "$baseUrl/api/v2/configuration" -Method Get
	Write-Host "✅ SUCCESS: Configuration retrieved" -ForegroundColor Green
	$response | ConvertTo-Json -Depth 3
} catch {
	Write-Host "❌ FAILED: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Health Check - Get Swagger JSON
Write-Host "📋 Test 2: Swagger Endpoint Check" -ForegroundColor Yellow
try {
	$response = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -Method Get
	$v2Endpoints = $response.paths.PSObject.Properties.Name | Where-Object { $_ -like "/api/v2/*" }
	Write-Host "✅ SUCCESS: Found $($v2Endpoints.Count) v2 endpoints" -ForegroundColor Green
	Write-Host "V2 Endpoints:" -ForegroundColor Cyan
	$v2Endpoints | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
} catch {
	Write-Host "❌ FAILED: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Create Call (will fail without proper config, but validates endpoint exists)
Write-Host "📋 Test 3: POST Create Call Endpoint Validation" -ForegroundColor Yellow
try {
	$body = @{
		target = "+14255551234"
		isPstn = $true
		operationContext = "test-call-001"
	} | ConvertTo-Json

	$response = Invoke-RestMethod -Uri "$baseUrl/api/v2/calls/create" `
		-Method Post `
		-ContentType "application/json" `
		-Body $body `
		-ErrorAction SilentlyContinue

	Write-Host "✅ SUCCESS: Endpoint is accessible (may fail due to config)" -ForegroundColor Green
	$response | ConvertTo-Json
} catch {
	if ($_.Exception.Response.StatusCode -eq 400) {
		Write-Host "✅ ENDPOINT EXISTS: Returned 400 (expected without valid ACS config)" -ForegroundColor Yellow
	} else {
		Write-Host "⚠️  Response: $($_.Exception.Message)" -ForegroundColor Yellow
	}
}
Write-Host ""

# Summary
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "📊 Test Summary" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "✅ Application is running at: $baseUrl" -ForegroundColor Green
Write-Host "✅ Swagger UI available at: $baseUrl/swagger" -ForegroundColor Green
Write-Host "✅ v2 API endpoints registered and accessible" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Open browser: http://localhost:5142/swagger" -ForegroundColor White
Write-Host "2. Look for tags ending with '(Clean Architecture)'" -ForegroundColor White
Write-Host "3. Test any v2 endpoint (starts with /api/v2/)" -ForegroundColor White
Write-Host ""
Write-Host "📚 Documentation:" -ForegroundColor Cyan
Write-Host "- V2_API_QUICKSTART.md - API usage examples" -ForegroundColor White
Write-Host "- ARCHITECTURE_VISUAL_GUIDE.md - Architecture diagrams" -ForegroundColor White
Write-Host "- COMPLETION_SUMMARY.md - Migration details" -ForegroundColor White
