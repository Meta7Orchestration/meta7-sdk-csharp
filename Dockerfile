FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore meta7-sdk-csharp.sln
RUN dotnet publish src/META7.Operator.Api/META7.Operator.Api.csproj -c Release -o /app/publish --no-restore
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global Microsoft.Playwright.CLI
RUN playwright install --with-deps chromium

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libc6 \
    libcairo2 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libexpat1 \
    libfontconfig1 \
    libgcc1 \
    libgbm1 \
    libglib2.0-0 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libpango-1.0-0 \
    libstdc++6 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxrandr2 \
    xdg-utils && \
    rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV PLAYWRIGHT_BROWSERS_PATH=/root/.cache/ms-playwright
EXPOSE 8080
COPY --from=build /app/publish .
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright
ENTRYPOINT ["dotnet", "META7.Operator.Api.dll"]
