FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["UCB_Hold.Server.csproj", "./"]
RUN dotnet restore "./UCB_Hold.Server.csproj"
COPY src/ ./src/
COPY appsettings.json ./
RUN dotnet publish "UCB_Hold.Server.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data-protection-keys && chown -R app:app /app/data-protection-keys
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "UCB_Hold.Server.dll"]
