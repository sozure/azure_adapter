
# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "./VGManager.Adapter.Api/VGManager.Adapter.Api.csproj"
RUN dotnet publish "./VGManager.Adapter.Api/VGManager.Adapter.Api.csproj" -c release -o /app --no-restore

# Serve Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./

EXPOSE 5004

ENTRYPOINT ["dotnet", "VGManager.Adapter.Api.dll"]
