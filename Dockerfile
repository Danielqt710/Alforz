# Imagen para publicar Alforz.Web (Render u otro host con Docker)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Alforz.Web/Alforz.Web.csproj src/Alforz.Web/
RUN dotnet restore src/Alforz.Web/Alforz.Web.csproj
COPY src/ src/
RUN dotnet publish src/Alforz.Web/Alforz.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# Render define PORT; si no está, se usa 8080 (el puerto por defecto de la imagen).
EXPOSE 8080
ENTRYPOINT ["dotnet", "Alforz.Web.dll"]
