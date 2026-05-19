FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrcamentoFamiliar.API/OrcamentoFamiliar.API.csproj", "OrcamentoFamiliar.API/"]
COPY ["OrcamentoFamiliar.Application/OrcamentoFamiliar.Application.csproj", "OrcamentoFamiliar.Application/"]
COPY ["OrcamentoFamiliar.Domain/OrcamentoFamiliar.Domain.csproj", "OrcamentoFamiliar.Domain/"]
COPY ["OrcamentoFamiliar.Infrastructure/OrcamentoFamiliar.Infrastructure.csproj", "OrcamentoFamiliar.Infrastructure/"]
RUN dotnet restore "OrcamentoFamiliar.API/OrcamentoFamiliar.API.csproj"
COPY . .
WORKDIR "/src/OrcamentoFamiliar.API"
RUN dotnet build "OrcamentoFamiliar.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrcamentoFamiliar.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrcamentoFamiliar.API.dll"]
