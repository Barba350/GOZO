FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY GOZO.csproj ./
RUN dotnet restore GOZO.csproj
COPY . ./
RUN dotnet publish GOZO.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
USER root
RUN apt-get update && apt-get install -y --no-install-recommends gosu && rm -rf /var/lib/apt/lists/*     && mkdir -p /data && chown -R app:app /data
COPY deploy/entrypoint.sh /usr/local/bin/gozo-entrypoint
RUN chmod 755 /usr/local/bin/gozo-entrypoint
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    GOZO_DATA_DIR=/data
EXPOSE 8080
ENTRYPOINT ["/usr/local/bin/gozo-entrypoint"]
