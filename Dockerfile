FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "./DeltGeometriFelleskomponent.Api/DeltGeometriFelleskomponent.Api.csproj"
RUN dotnet build "./DeltGeometriFelleskomponent.Api/DeltGeometriFelleskomponent.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./DeltGeometriFelleskomponent.Api/DeltGeometriFelleskomponent.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeltGeometriFelleskomponent.Api.dll"]