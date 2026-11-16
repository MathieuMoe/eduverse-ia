# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/EduVerse.Web/EduVerse.Web.csproj -c Release -o /app/publish

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# SQLite database folder, writable by the non-root user (mounted as a volume in docker-compose)
RUN mkdir -p /app/App_Data && chown $APP_UID /app/App_Data
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "EduVerse.Web.dll"]
