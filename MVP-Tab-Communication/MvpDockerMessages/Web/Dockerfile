FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Web/MvpDockerMessages.Web.csproj Web/
RUN dotnet restore Web/MvpDockerMessages.Web.csproj

COPY Web/ Web/
RUN dotnet publish Web/MvpDockerMessages.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MvpDockerMessages.Web.dll"]
