# Throwaway test image: restores packages once, then runs the Proxmox tests.
# Build context is this folder. Run through scripts/run-proxmox-tests.sh, which
# passes PVE_* settings and the per-run token as environment variables.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:747cbade5bfd918a175ac32f8f1d931edec2c7d0d5397a3de72eaef1ac1f66d2
# The packages go under /tests, so the non-root user below can read them.
ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    NUGET_PACKAGES=/tests/.nuget/packages
WORKDIR /tests
# xUnit v3 runs on Microsoft.Testing.Platform; on the .NET 10 SDK `dotnet test` needs
# this opt-in, which the global.json of this folder gives a local run.
RUN echo '{"test":{"runner":"Microsoft.Testing.Platform"}}' > global.json
COPY NFHotel.Proxmox.Tests.csproj packages.lock.json ./
# The lock file pins every package by version and content hash (MIL-022 task 4).
RUN dotnet restore --locked-mode
COPY *.cs ./
RUN dotnet build --no-restore && chown -R app:app /tests
# The tests run as the image's non-root user `app`, which also writes the test results.
USER app
ENTRYPOINT ["dotnet", "test", "--no-build"]
