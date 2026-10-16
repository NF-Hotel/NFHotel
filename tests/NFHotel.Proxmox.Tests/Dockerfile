# Throwaway test image: restores packages once, then runs the Proxmox tests.
# Build context is this folder. Run through scripts/run-proxmox-tests.sh, which
# passes PVE_* settings and the per-run token as environment variables.
FROM mcr.microsoft.com/dotnet/sdk:10.0
ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /tests
COPY NFHotel.Proxmox.Tests.csproj ./
RUN dotnet restore
COPY *.cs ./
RUN dotnet build --no-restore
ENTRYPOINT ["dotnet", "test", "--no-build", "--logger", "console;verbosity=normal"]
