FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto primero para aprovechar el cache de capas
COPY MiniStock.sln ./
COPY api/MiniStock.Domain/MiniStock.Domain.csproj           api/MiniStock.Domain/
COPY api/MiniStock.Application/MiniStock.Application.csproj api/MiniStock.Application/
COPY api/MiniStock.Infrastructure/MiniStock.Infrastructure.csproj api/MiniStock.Infrastructure/
COPY api/MiniStock.Api/MiniStock.Api.csproj                 api/MiniStock.Api/

RUN dotnet restore api/MiniStock.Api/MiniStock.Api.csproj

COPY . .

RUN dotnet publish api/MiniStock.Api/MiniStock.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

# No correr como root dentro del contenedor — la imagen base ya trae un
# usuario sin privilegios listo para usar via la env var $APP_UID.
USER $APP_UID

ENTRYPOINT ["dotnet", "MiniStock.Api.dll"]
