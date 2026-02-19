# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ResultViewer.Domain/ResultViewer.Domain.csproj ResultViewer.Domain/
COPY ResultViewer.Application/ResultViewer.Application.csproj ResultViewer.Application/
COPY ResultViewer.Infrastructure/ResultViewer.Infrastructure.csproj ResultViewer.Infrastructure/
COPY ResultViewer.Api/ResultViewer.Api.csproj ResultViewer.Api/
RUN dotnet restore ResultViewer.Api/ResultViewer.Api.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish ResultViewer.Api/ResultViewer.Api.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "ResultViewer.Api.dll"]
