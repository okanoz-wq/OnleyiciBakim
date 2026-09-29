FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OnleyiciBakim/OnleyiciBakim.csproj", "OnleyiciBakim/"]
RUN dotnet restore "OnleyiciBakim/OnleyiciBakim.csproj"
COPY . .
RUN dotnet publish "OnleyiciBakim/OnleyiciBakim.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OnleyiciBakim.dll"]
