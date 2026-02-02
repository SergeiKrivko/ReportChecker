FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ReportChecker.Api/ReportChecker.Api.csproj", "ReportChecker.Api/"]
COPY ["ReportChecker.Abstractions/ReportChecker.Abstractions.csproj", "ReportChecker.Abstractions/"]
COPY ["ReportChecker.Models/ReportChecker.Models.csproj", "ReportChecker.Models/"]
COPY ["ReportChecker.DataAccess/ReportChecker.DataAccess.csproj", "ReportChecker.DataAccess/"]
COPY ["ReportChecker.Application/ReportChecker.Application.csproj", "ReportChecker.Application/"]
COPY ["AuthProviders/ReportChecker.AuthProviders.Yandex/ReportChecker.AuthProviders.Yandex.csproj", "AuthProviders/ReportChecker.AuthProviders.Yandex/"]
COPY ["SourceProviders/ReportChecker.SourceProviders.File/ReportChecker.SourceProviders.File.csproj", "SourceProviders/ReportChecker.SourceProviders.File/"]
COPY ["FormatProviders/ReportChecker.FormatProviders.Latex/ReportChecker.FormatProviders.Latex.csproj", "FormatProviders/ReportChecker.FormatProviders.Latex/"]
COPY ["ReportChecker.AiAgent/ReportChecker.AiAgent.csproj", "ReportChecker.AiAgent/"]
COPY ["ReportChecker.S3/ReportChecker.S3.csproj", "ReportChecker.S3/"]
RUN dotnet restore "ReportChecker.Api/ReportChecker.Api.csproj"
COPY . .
WORKDIR "/src/ReportChecker.Api"
RUN dotnet build "./ReportChecker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ReportChecker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8081;3000.
ENTRYPOINT ["dotnet", "ReportChecker.Api.dll"]
EXPOSE 3000
