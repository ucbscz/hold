FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["IMT_Reservas.Server.csproj", "./"]
RUN dotnet restore "./IMT_Reservas.Server.csproj"
COPY src/ ./src/
COPY Properties/ ./Properties/
COPY appsettings*.json ./
RUN dotnet publish "IMT_Reservas.Server.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data-protection-keys && chown -R app:app /app/data-protection-keys
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "IMT_Reservas.Server.dll"]
