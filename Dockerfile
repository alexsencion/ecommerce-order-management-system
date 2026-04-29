FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

COPY ["ECommerce.API/ECommerce.API.csproj",             "ECommerce.API/"]
COPY ["ECommerce.Application/ECommerce.Application.csproj", "ECommerce.Application/"]
COPY ["ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "ECommerce.Infrastructure/"]
COPY ["ECommerce.Domain/ECommerce.Domain.csproj",           "ECommerce.Domain/"]

RUN dotnet restore "ECommerce.API/ECommerce.API.csproj"

COPY . .
RUN dotnet build "ECommerce.API/ECommerce.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ECommerce.API/ECommerce.API.csproj" -C Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT [ "dotnet", "ECommerce.API.dll" ]