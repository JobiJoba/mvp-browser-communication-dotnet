FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Api/MvpDockerMessages.Api.csproj Api/
RUN dotnet restore Api/MvpDockerMessages.Api.csproj

COPY Api/ Api/
RUN dotnet publish Api/MvpDockerMessages.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MvpDockerMessages.Api.dll"]
