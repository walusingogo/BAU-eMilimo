# ============================================
# DEVELOPMENT STAGE - With Hot Reload
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /src

# Copy solution and project files
COPY eMilimo.sln .
COPY eMilimo.API/*.csproj eMilimo.API/
COPY eMilimo.Core/*.csproj eMilimo.Core/
COPY eMilimo.Infrastructure/*.csproj eMilimo.Infrastructure/

# Restore dependencies
RUN dotnet restore eMilimo.sln

# Copy all source code
COPY . .

# Install dotnet watch for hot reload
RUN dotnet tool install -g dotnet-watch
ENV PATH="${PATH}:/root/.dotnet/tools"

# Ensure wwwroot/uploads exists
RUN mkdir -p /src/eMilimo.API/wwwroot/uploads

EXPOSE 5001
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:5001

# Use dotnet watch for hot reload
CMD ["dotnet", "watch", "run", "--project", "eMilimo.API/eMilimo.API.csproj", "--urls=http://+:5001", "--no-launch-profile"]
