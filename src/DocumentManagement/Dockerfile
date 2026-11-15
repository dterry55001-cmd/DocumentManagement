FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/DocumentManagement/DocumentManagement.csproj src/DocumentManagement/
RUN dotnet restore src/DocumentManagement/DocumentManagement.csproj
COPY . .
WORKDIR /src/src/DocumentManagement
RUN dotnet publish DocumentManagement.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
RUN mkdir -p /app/data/documents /app/data/temp && chown -R $APP_UID:$APP_UID /app
USER $APP_UID
ENTRYPOINT ["dotnet", "DocumentManagement.dll"]
