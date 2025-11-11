FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/MagmaAssessment.API/MagmaAssessment.API.csproj", "src/MagmaAssessment.API/"]
COPY ["src/MagmaAssessment.Core/MagmaAssessment.Core.csproj", "src/MagmaAssessment.Core/"]
COPY ["src/MagmaAssessment.Infrastructure/MagmaAssessment.Infrastructure.csproj", "src/MagmaAssessment.Infrastructure/"]

RUN dotnet restore "src/MagmaAssessment.API/MagmaAssessment.API.csproj"

COPY . .

WORKDIR "/src/src/MagmaAssessment.API"
RUN dotnet build "MagmaAssessment.API.csproj" -c Release -o /app/build

RUN dotnet publish "MagmaAssessment.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
USER appuser

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "MagmaAssessment.API.dll"]