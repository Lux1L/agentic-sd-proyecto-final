# Build del SPA con pnpm y lo sirve nginx; /api se reenvía a la API (ver nginx.conf).
FROM node:24-alpine AS build
WORKDIR /app
RUN npm install -g pnpm@12.4.2
COPY package.json pnpm-lock.yaml ./
RUN pnpm ci
COPY . .
RUN pnpm build

# Imagen sin root: nginx corre como el usuario nginx (uid 101) y escucha en 8080.
FROM nginxinc/nginx-unprivileged:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
USER nginx
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s CMD wget -q --spider http://127.0.0.1:8080/ || exit 1
