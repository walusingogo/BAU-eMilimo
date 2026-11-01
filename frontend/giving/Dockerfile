FROM node:22-alpine AS builder
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=builder /src/dist/giving/browser/ /usr/share/nginx/html/
COPY nginx.production.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
