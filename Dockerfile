FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/TexasPortal.Api/TexasPortal.Api.csproj", "src/TexasPortal.Api/"]
COPY ["src/TexasPortal.Application/TexasPortal.Application.csproj", "src/TexasPortal.Application/"]
COPY ["src/TexasPortal.Infrastructure/TexasPortal.Infrastructure.csproj", "src/TexasPortal.Infrastructure/"]
COPY ["src/TexasPortal.Domain/TexasPortal.Domain.csproj", "src/TexasPortal.Domain/"]
COPY ["src/TexasPortal.Contracts/TexasPortal.Contracts.csproj", "src/TexasPortal.Contracts/"]
RUN dotnet restore "src/TexasPortal.Api/TexasPortal.Api.csproj"
COPY . .
WORKDIR "/src/src/TexasPortal.Api"
RUN dotnet publish "TexasPortal.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TexasPortal.Api.dll"]
