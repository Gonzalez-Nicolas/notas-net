FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/NotasNet.Api/NotasNet.Api.csproj src/NotasNet.Api/
RUN dotnet restore src/NotasNet.Api/NotasNet.Api.csproj
COPY src/ src/
RUN dotnet publish src/NotasNet.Api/NotasNet.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "NotasNet.Api.dll"]
