FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine AS base
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apk add --no-cache icu-libs
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Spard.Service/Spard.Service.csproj", "src/Spard.Service/"]
COPY ["src/Spard.Service.Contract/Spard.Service.Contract.csproj", "src/Spard.Service.Contract/"]
COPY ["src/Spard/Spard.csproj", "src/Spard/"]
RUN dotnet restore "src/Spard.Service/Spard.Service.csproj" -r linux-musl-x64
COPY . .
WORKDIR "/src/src/Spard.Service"
RUN dotnet publish "Spard.Service.csproj" -p:PublishSingleFile=true -r linux-musl-x64 -p:PublishTrimmed=true -p:InvariantGlobalization=false -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./Spard.Service"]