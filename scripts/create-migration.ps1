# Run this script from the repository root to create the initial EF Core migration
# Requires .NET 8 SDK and dotnet-ef tool installed:
#   dotnet tool install --global dotnet-ef

Set-Location "$PSScriptRoot\..\backend"

Write-Host "Creating initial migration..." -ForegroundColor Cyan

dotnet ef migrations add InitialCreate `
  --project OrcamentoFamiliar.Infrastructure `
  --startup-project OrcamentoFamiliar.API `
  --context AppDbContext

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration created! Applying to database..." -ForegroundColor Green
    dotnet ef database update `
      --project OrcamentoFamiliar.Infrastructure `
      --startup-project OrcamentoFamiliar.API
    Write-Host "Done." -ForegroundColor Green
} else {
    Write-Host "Migration failed." -ForegroundColor Red
}
