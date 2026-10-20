# Builds the application with the same SDK version as in global.json.
# Base images pinned by digest (MIL-022 task 4); Dependabot proposes the next ones.
FROM mcr.microsoft.com/dotnet/sdk:10.0.400@sha256:4beef5b8919dcaa2dc924233bd069257e883cc7a061e09088a97d152d6a48510 AS build

# Arbejdsmappe i build-imaget.
WORKDIR /source

# Copies the SDK settings, the code rules and the four projects under src.
COPY global.json .editorconfig ./
COPY src/ ./src/

# Fetches the NuGet packages, builds and publishes the web application for deployment.
RUN dotnet publish src/NFHotel.Web/NFHotel.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# The final image contains ASP.NET Core to run the application.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.13@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac AS final

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
