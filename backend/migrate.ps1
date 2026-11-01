# backend/migrate.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$eMilimo
)

dotnet ef migrations add $eMilimo --project eMilimo.Infrastructure --startup-project eMilimo.API
dotnet ef database update --project eMilimo.Infrastructure --startup-project eMilimo.API