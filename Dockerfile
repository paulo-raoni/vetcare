# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/VetCare.Domain/VetCare.Domain.csproj         src/VetCare.Domain/
COPY src/VetCare.Application/VetCare.Application.csproj src/VetCare.Application/
COPY src/VetCare.Infrastructure/VetCare.Infrastructure.csproj src/VetCare.Infrastructure/
COPY src/VetCare.Api/VetCare.Api.csproj               src/VetCare.Api/

RUN dotnet restore src/VetCare.Api/VetCare.Api.csproj

COPY src/ src/
RUN dotnet publish src/VetCare.Api/VetCare.Api.csproj \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS final
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "VetCare.Api.dll"]
