#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Spard.Service/Spard.Service.csproj", "src/Spard.Service/"]
COPY ["src/Spard.Service.Contract/Spard.Service.Contract.csproj", "src/Spard.Service.Contract/"]
COPY ["src/Spard/Spard.csproj", "src/Spard/"]
RUN dotnet restore "src/Spard.Service/Spard.Service.csproj"
COPY . .
WORKDIR "/src/src/Spard.Service"
RUN dotnet build "Spard.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Spard.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Spard.Service.dll"]