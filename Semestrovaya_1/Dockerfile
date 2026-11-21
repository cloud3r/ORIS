FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY CardSite/CardSite.csproj CardSite/
COPY MiniTemplateEngine/MiniTemplateEngine.csproj MiniTemplateEngine/

RUN dotnet restore CardSite/CardSite.csproj

COPY CardSite/ CardSite/
COPY MiniTemplateEngine/ MiniTemplateEngine/

RUN dotnet publish CardSite/CardSite.csproj -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish .

COPY CardSite/Views ./Views

COPY CardSite/wwwroot ./wwwroot

EXPOSE 8080

ENTRYPOINT ["dotnet", "CardSite.dll"]