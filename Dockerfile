FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["nng-server.csproj", "./"]
RUN dotnet restore "nng-server.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "nng-server.csproj" -c Release -o /app/build --no-self-contained

FROM build AS publish
RUN dotnet publish "nng-server.csproj" -c Release -o /app/publish --no-self-contained

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "nng-server.dll"]
