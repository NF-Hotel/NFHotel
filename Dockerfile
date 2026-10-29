# Builds the application with the same SDK version as in global.json.
# Base images pinned by digest (MIL-022 task 4); Dependabot proposes the next ones.
FROM mcr.microsoft.com/dotnet/sdk:10.0.403@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build

# Working folder of the build image.
WORKDIR /source

# Copies the SDK settings, the code rules and the four projects under src.
COPY global.json .editorconfig ./
COPY src/ ./src/

# Fetches the NuGet packages, builds and publishes the web application for deployment.
RUN dotnet publish src/NFHotel.Web/NFHotel.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# The final image contains ASP.NET Core to run the application.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14@sha256:de0ee358cdd672f7017a003bed21e9ac9ed48a27c922a59b18175b6e82fa7fee AS final

# Working folder of the final image.
WORKDIR /app

# The security updates that Ubuntu has published since the base image was built, so a fixed
# package does not wait for the next base image (ADR-0032: a finding with a fix is acted on).
RUN apt-get update \
    && apt-get upgrade --yes --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# Copies the finished application from the build image.
COPY --from=build /app/publish .

# The Data Protection keys live in /app/keys, which the deployment mounts as a volume, so the staff cookie and the
# antiforgery tokens survive a new container (ADR-0031). The folder belongs to the user app; a new volume takes its
# owner from it.
RUN mkdir /app/keys && chown app:app /app/keys && chmod 0700 /app/keys
ENV DataProtection__KeysPath=/app/keys

# The application listens on port 8080 inside the container.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Runs the application as the built-in user, without root rights.
USER app

# Starts NFHotel.Web when the container starts.
ENTRYPOINT ["dotnet", "NFHotel.Web.dll"]
