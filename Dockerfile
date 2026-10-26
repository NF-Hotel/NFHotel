# Builds the application with the same SDK version as in global.json.
# Base images pinned by digest (MIL-022 task 4); Dependabot proposes the next ones.
FROM mcr.microsoft.com/dotnet/sdk:10.0.402@sha256:747cbade5bfd918a175ac32f8f1d931edec2c7d0d5397a3de72eaef1ac1f66d2 AS build

# Arbejdsmappe i build-imaget.
WORKDIR /source

# Copies the SDK settings, the code rules and the four projects under src.
COPY global.json .editorconfig ./
COPY src/ ./src/

# Fetches the NuGet packages, builds and publishes the web application for deployment.
RUN dotnet publish src/NFHotel.Web/NFHotel.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# The final image contains ASP.NET Core to run the application.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14@sha256:d247d17dd429d96580eeb96cfce220c1089d860dbbf9f94d599ca64cf3604fe4 AS final

# Arbejdsmappe i det endelige image.
WORKDIR /app

# Copies the finished application from the build image.
COPY --from=build /app/publish .

# The application listens on port 8080 inside the container.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Runs the application as the built-in user, without root rights.
USER app

# Starts NFHotel.Web when the container starts.
ENTRYPOINT ["dotnet", "NFHotel.Web.dll"]
