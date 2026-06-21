FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/SanctionsAlertService.Api/SanctionsAlertService.Api.csproj", "src/SanctionsAlertService.Api/"]
COPY ["src/SanctionsAlertService.Application/SanctionsAlertService.Application.csproj", "src/SanctionsAlertService.Application/"]
COPY ["src/SanctionsAlertService.Infrastructure/SanctionsAlertService.Infrastructure.csproj", "src/SanctionsAlertService.Infrastructure/"]
COPY ["src/SanctionsAlertService.Domain/SanctionsAlertService.Domain.csproj", "src/SanctionsAlertService.Domain/"]

RUN dotnet restore "src/SanctionsAlertService.Api/SanctionsAlertService.Api.csproj"

COPY . .
WORKDIR /src/src/SanctionsAlertService.Api
RUN dotnet publish "SanctionsAlertService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SanctionsAlertService.Api.dll"]
