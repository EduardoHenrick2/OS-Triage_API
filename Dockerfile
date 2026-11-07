FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/OsTriageApi/OsTriageApi.csproj src/OsTriageApi/
RUN dotnet restore src/OsTriageApi/OsTriageApi.csproj

COPY src/OsTriageApi/ src/OsTriageApi/
RUN dotnet publish src/OsTriageApi/OsTriageApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

USER app
EXPOSE 5197
ENTRYPOINT ["dotnet", "OsTriageApi.dll"]
