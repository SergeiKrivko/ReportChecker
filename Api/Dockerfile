FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Api/Api.csproj", "Api/"]
COPY ["Abstractions/Abstractions.csproj", "Abstractions/"]
COPY ["Models/Models.csproj", "Models/"]
COPY ["DataAccess/DataAccess.csproj", "DataAccess/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["SourceProviders/SourceProviders.File/SourceProviders.File.csproj", "SourceProviders/SourceProviders.File/"]
COPY ["SourceProviders/SourceProviders.GitHub/SourceProviders.GitHub.csproj", "SourceProviders/SourceProviders.GitHub/"]
COPY ["FormatProviders/FormatProviders.Latex/FormatProviders.Latex.csproj", "FormatProviders/FormatProviders.Latex/"]
COPY ["FormatProviders/FormatProviders.Pdf/FormatProviders.Pdf.csproj", "FormatProviders/FormatProviders.Pdf/"]
COPY ["AiAgent/AiAgent.csproj", "AiAgent/"]
COPY ["S3/S3.csproj", "S3/"]
RUN dotnet restore "Api/Api.csproj"
COPY . .
WORKDIR "/src/Api"
RUN dotnet build "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8081;3000
ENTRYPOINT ["dotnet", "Api.dll"]
EXPOSE 3000
