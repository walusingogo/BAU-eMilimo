# ============================================
# DEVELOPMENT STAGE - With Hot Reload
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
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

# Install dotnet watch for hot reload
RUN dotnet tool install -g dotnet-watch
ENV PATH="${PATH}:/root/.dotnet/tools"

# Ensure wwwroot/uploads exists
RUN mkdir -p /src/UCP.API/wwwroot/uploads

EXPOSE 5001
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:5001

# Use dotnet watch for hot reload
CMD ["dotnet", "watch", "run", "--project", "UCP.API/UCP.API.csproj", "--urls=http://+:5001", "--no-launch-profile"]