# Build the .NET 9 API and Blazor WebAssembly client in one image.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish TripPlanner.Client/TripPlanner.Client.csproj -c Release -o /out/client
RUN dotnet publish TripPlanner.Api/TripPlanner.Api.csproj -c Release -o /out/api

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out/api/ ./
COPY --from=build /out/client/wwwroot/ ./wwwroot/
# In production the API and website share the same HTTPS origin.
RUN printf '{"ApiBaseUrl":"/"}\n' > ./wwwroot/appsettings.json
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "TripPlanner.Api.dll"]
