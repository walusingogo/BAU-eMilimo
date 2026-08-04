# backend/migrate.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$UCP
)

dotnet ef migrations add $UCP --project UCP.Infrastructure --startup-project UCP.API
dotnet ef database update --project UCP.Infrastructure --startup-project UCP.API