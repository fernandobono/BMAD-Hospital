# Contexto = raíz del repo: el build necesita los Directory.*.props y los otros proyectos de src/.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /fuente
COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet restore src/HospitalTurnos.Web/HospitalTurnos.Web.csproj
RUN dotnet publish src/HospitalTurnos.Web/HospitalTurnos.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# M-11: carpeta de claves de protección de datos. Un volumen nombrado nuevo copia el dueño de la
# carpeta de la imagen, así el usuario sin privilegios puede escribir las claves.
RUN mkdir -p /claves-proteccion && chown $APP_UID /claves-proteccion
USER $APP_UID
COPY --from=compilacion /app .
# La imagen escucha en 8080 por defecto (ASPNETCORE_HTTP_PORTS).
ENTRYPOINT ["dotnet", "HospitalTurnos.Web.dll"]
