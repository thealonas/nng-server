FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine AS base
WORKDIR /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview-alpine AS build
ARG TARGETARCH
WORKDIR /src
COPY ["nng-server.csproj", "."]
RUN dotnet restore -a $TARGETARCH "./nng-server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "nng-server.csproj" -c Release -o /app/build --no-self-contained

FROM build AS publish
RUN dotnet publish "nng-server.csproj" -c Release -a $TARGETARCH -o /app/publish --no-self-contained

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "nng-server.dll"]
