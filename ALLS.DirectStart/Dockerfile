FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ALLS.DirectStart/ALLS.DirectStart.csproj", "ALLS.DirectStart/"]
RUN dotnet restore "ALLS.DirectStart/ALLS.DirectStart.csproj"
COPY . .
WORKDIR "/src/ALLS.DirectStart"
RUN dotnet build "ALLS.DirectStart.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ALLS.DirectStart.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ALLS.DirectStart.dll"]
