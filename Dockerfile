# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["signalR_server.csproj", "."]
RUN dotnet restore "signalR_server.csproj"

COPY . .

RUN dotnet publish "signalR_server.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "signalR_server.dll"]