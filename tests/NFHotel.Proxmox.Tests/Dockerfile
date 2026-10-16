# Throwaway test image: restores packages once, then runs the Proxmox tests.
# Build context is this folder. Run with: docker run --rm --env-file pve.env
FROM mcr.microsoft.com/dotnet/sdk:10.0
ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /tests
COPY NFHotel.Proxmox.Tests.csproj ./
RUN dotnet restore
COPY *.cs ./
RUN dotnet build --no-restore
ENTRYPOINT ["dotnet", "test", "--no-build", "--logger", "console;verbosity=normal"]
