# Render.com / Docker production image (build from repository root)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/HieuNga.Web/HieuNga.Web.csproj
RUN dotnet publish src/HieuNga.Web/HieuNga.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
# Render sets PORT at runtime; Program.cs also reads PORT if present
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HieuNga.Web.dll"]
