# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/API/RAGChatbot.API.csproj", "src/API/"]
COPY ["src/Core/RAGChatbot.Core.csproj", "src/Core/"]
COPY ["src/Infrastructure/RAGChatbot.Infrastructure.csproj", "src/Infrastructure/"]

RUN dotnet restore "src/API/RAGChatbot.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/API"
RUN dotnet publish "RAGChatbot.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install SQLite
RUN apt-get update && apt-get install -y sqlite3 libsqlite3-dev && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create volume for persistent data
VOLUME ["/app/data"]

# Set environment variable for database path
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/ragchatbot.db"

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "RAGChatbot.API.dll"]