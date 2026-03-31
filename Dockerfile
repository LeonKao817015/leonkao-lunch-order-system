# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["leonkao-lunch-order-system/leonkao-lunch-order-system.csproj", "leonkao-lunch-order-system/"]
RUN dotnet restore "leonkao-lunch-order-system/leonkao-lunch-order-system.csproj"

COPY leonkao-lunch-order-system/ leonkao-lunch-order-system/
RUN dotnet publish "leonkao-lunch-order-system/leonkao-lunch-order-system.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# 建立上傳資料夾（Render 免費方案 filesystem 是暫時性的，重啟後清空）
RUN mkdir -p wwwroot/uploads

COPY --from=build /app/publish .

# ── GC 調優：限制 heap 在 350MB，保留 OS 約 160MB buffer ──────────────────
# DOTNET_GCConserveMemory=9 = 最積極的記憶體節省模式
# DOTNET_GCHeapHardLimit = 350MB (367001600 bytes)
ENV DOTNET_GCConserveMemory=9 \
    DOTNET_GCHeapHardLimit=367001600 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    ASPNETCORE_ENVIRONMENT=Production

# Render 會在啟動時注入 PORT 環境變數，預設 8080 給本地測試用
ENV PORT=8080
EXPOSE 8080

CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT} dotnet leonkao-lunch-order-system.dll"]
