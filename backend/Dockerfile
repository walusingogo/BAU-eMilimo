# ============================================
# STAGE 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY UCP.sln .
COPY UCP.API/*.csproj UCP.API/
COPY UCP.Core/*.csproj UCP.Core/
COPY UCP.Infrastructure/*.csproj UCP.Infrastructure/

# Restore dependencies
RUN dotnet restore UCP.sln

# Copy all source code
COPY . .

# Build the API
WORKDIR /src/UCP.API
RUN dotnet build -c Release -o /app/build

# ============================================
# STAGE 2: Publish
# ============================================
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# ============================================
# STAGE 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Set environment variable to use port 5001
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5001

ENTRYPOINT ["dotnet", "UCP.API.dll"]