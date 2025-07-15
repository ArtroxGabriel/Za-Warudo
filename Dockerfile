# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY *.sln ./
COPY ZaWarudo/*.csproj ./ZaWarudo/
COPY ZaWarudo.Tests/*.csproj ./ZaWarudo.Tests/
RUN dotnet restore

# Copy source code and build
COPY . .
WORKDIR /src/ZaWarudo
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official .NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy the published application first
COPY --from=build /app/publish .

# Create necessary directories for runtime
RUN mkdir -p /app/logs /app/storage /app/data

# Copy the wines.csv to a different location to avoid conflicts
# FIX: ajeitar
COPY --from=build /src/ZaWarudo/Data/wines.csv /app/data/

# Copy appsettings.json from the YggdrasilVinum directory
COPY --from=build /src/ZaWarudo/appsettings.json .

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=Production

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && \
    chown -R appuser:appuser /app
USER appuser

# Set the entrypoint with the wine data file path as an argument
ENTRYPOINT ["dotnet", "ZaWarudo.dll"]
#CMD ["/app/data/wines.csv"]
