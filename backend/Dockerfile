FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/AuctionNest.API/AuctionNest.API.csproj",           "src/AuctionNest.API/"]
COPY ["src/AuctionNest.Infrastructure/AuctionNest.Infrastructure.csproj", "src/AuctionNest.Infrastructure/"]
COPY ["src/AuctionNest.Application/AuctionNest.Application.csproj",   "src/AuctionNest.Application/"]
COPY ["src/AuctionNest.Domain/AuctionNest.Domain.csproj",     "src/AuctionNest.Domain/"]

RUN dotnet restore "src/AuctionNest.API/AuctionNest.API.csproj"

COPY . .
WORKDIR "/src/src/AuctionNest.API"
RUN dotnet publish "AuctionNest.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuctionNest.API.dll"]