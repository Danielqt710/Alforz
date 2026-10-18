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
# La app escucha en PORT (Render pone el suyo; si no, 8080). Se vacía HTTP_PORTS de la imagen
# para que no avise "Overriding HTTP_PORTS" en cada arranque.
ENV PORT=8080
ENV ASPNETCORE_HTTP_PORTS=
EXPOSE 8080
ENTRYPOINT ["dotnet", "Alforz.Web.dll"]
