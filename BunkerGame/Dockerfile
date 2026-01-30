# 1. Этап сборки (Build Stage)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["BunkerGame/BunkerGame.csproj", "BunkerGame/"]
RUN dotnet restore "BunkerGame/BunkerGame.csproj"

# Копируем всё остальное и собираем
COPY . .
WORKDIR "/src/BunkerGame"
RUN dotnet build "BunkerGame.csproj" -c Release -o /app/build

# Публикуем (готовим файлы для запуска)
FROM build AS publish
RUN dotnet publish "BunkerGame.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Этап запуска (Runtime Stage)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# Копируем скомпилированные файлы из этапа сборки
COPY --from=publish /app/publish .

# Создаем папку для аватарок, если её нет (внутри контейнера)
RUN mkdir -p wwwroot/avatars

ENTRYPOINT ["dotnet", "BunkerGame.dll"]