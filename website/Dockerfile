# Build: node 22 → static dist/. Runtime: nginx serving the SPA with a
# runtime-generated /config.js (docs/12 §F21.10).
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY docker/40-config.sh /docker-entrypoint.d/40-config.sh
COPY --from=build /app/dist /usr/share/nginx/html
RUN chmod +x /docker-entrypoint.d/40-config.sh
EXPOSE 80
