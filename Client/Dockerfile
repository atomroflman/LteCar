FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
ARG SERVER_ORIGIN=http://server:5000
ARG JANUS_HTTP_ORIGIN=http://janus:8088
ARG JANUS_WS_ORIGIN=http://janus:8188
ENV SERVER_ORIGIN=$SERVER_ORIGIN
ENV JANUS_HTTP_ORIGIN=$JANUS_HTTP_ORIGIN
ENV JANUS_WS_ORIGIN=$JANUS_WS_ORIGIN
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
COPY --from=build /app/out/standalone ./
COPY --from=build /app/out/static ./out/static
COPY --from=build /app/public ./public
EXPOSE 3000
CMD ["node", "server.js"]
