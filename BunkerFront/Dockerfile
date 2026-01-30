# ЭТАП 1: Сборка
FROM node:20-alpine AS build
WORKDIR /app

# Копируем зависимости
COPY package*.json ./
RUN npm install

# Копируем код и собираем
COPY . .
RUN npm run build

# ЭТАП 3: Раздача статики через Nginx
FROM nginx:stable-alpine
# Копируем билд из первого этапа
COPY --from=build /app/dist /usr/share/nginx/html
# Копируем наш конфиг Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]