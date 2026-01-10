# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file
COPY ChoreNotifier/ChoreNotifier.csproj ./ChoreNotifier/

# Restore dependencies
RUN dotnet restore ChoreNotifier/ChoreNotifier.csproj

# Copy the rest of the code
COPY . .

# Build the application
WORKDIR /src/ChoreNotifier
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ChoreNotifier.dll"]
