# E2E Test Runner Script for LocalFinanceManager
# This script starts the API and Blazor servers, then runs Playwright e2e tests

Write-Host "🚀 LocalFinanceManager E2E Test Runner" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Kill any existing processes on the target ports (optional)
Write-Host "`n📌 Checking for existing processes on ports 5096, 5114, 7126, 7163..."
try {
    Get-NetTCPConnection -LocalPort 5096, 5114, 7126, 7163 -ErrorAction SilentlyContinue | ForEach-Object {
        $process = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
        if ($process) {
            Write-Host "⚠️  Found process on port $($_.LocalPort): $($process.Name) (PID: $($process.Id))"
        }
    }
}
catch { }

Write-Host "`n⏱️  Starting API server (Port 5096/7126)..."
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList @("run", "--project", "src/LocalFinanceManager.Api", "--no-build") -PassThru
Write-Host "✅ API server started (PID: $($apiProcess.Id))" -ForegroundColor Green

Write-Host "`n⏱️  Starting Blazor UI server (Port 5114/7163)..."
$blazorProcess = Start-Process -FilePath "dotnet" -ArgumentList @("run", "--project", "src/LocalFinanceManager.Blazor", "--no-build") -PassThru
Write-Host "✅ Blazor server started (PID: $($blazorProcess.Id))" -ForegroundColor Green

Write-Host "`n⏳ Waiting 20 seconds for servers to fully initialize..."
Start-Sleep -Seconds 20

Write-Host "`n🧪 Running Playwright E2E Tests..."
Write-Host "========================================`n" -ForegroundColor Green

try {
    dotnet test tests/LocalFinanceManager.E2E/ --logger console --verbosity detailed
    $testExitCode = $LASTEXITCODE
}
catch {
    Write-Host "❌ Error running tests: $_" -ForegroundColor Red
    $testExitCode = 1
}

Write-Host "`n🛑 Cleaning up: Stopping servers..."
Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $blazorProcess.Id -Force -ErrorAction SilentlyContinue
Write-Host "✅ Servers stopped" -ForegroundColor Green

if ($testExitCode -eq 0) {
    Write-Host "`n✨ All tests passed!" -ForegroundColor Green
}
else {
    Write-Host "`n❌ Some tests failed (Exit code: $testExitCode)" -ForegroundColor Red
}

exit $testExitCode
