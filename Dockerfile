FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/CqrsDemo.Api/CqrsDemo.Api.csproj .
RUN dotnet restore
COPY src/CqrsDemo.Api/ .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 5000
ENTRYPOINT ["dotnet", "CqrsDemo.Api.dll"]
