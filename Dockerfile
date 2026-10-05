# Bygger appen med samme SDK-version som i global.json.
FROM mcr.microsoft.com/dotnet/sdk:10.0.400 AS build

# Arbejdsmappe i build-imaget.
WORKDIR /source

# Kopierer SDK-indstillinger, koderegler og de fire projekter under src.
COPY global.json .editorconfig ./
COPY src/ ./src/

# Henter NuGet-pakker, bygger og pakker webappen til deployment.
RUN dotnet publish src/NFHotel.Web/NFHotel.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Det endelige image indeholder ASP.NET Core til at køre appen.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Arbejdsmappe i det endelige image.
WORKDIR /app

# Kopierer den færdige app fra build-imaget.
COPY --from=build /app/publish .

# Appen lytter på port 8080 inde i containeren.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Kører appen som den indbyggede bruger uden root-rettigheder.
USER app

# Starter NFHotel.Web, når containeren starter.
ENTRYPOINT ["dotnet", "NFHotel.Web.dll"]
