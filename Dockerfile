
#  restaurar dependências
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY GFGestao.API.csproj ./
RUN dotnet restore GFGestao.API.csproj

#  compilar e publicar
COPY . ./
RUN dotnet publish GFGestao.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

#  imagem de execução
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Executar com usuário não privilegiado
USER $APP_UID

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish ./

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/health/ready || exit 1

  ENTRYPOINT ["dotnet", "GFGestao.API.dll"]

