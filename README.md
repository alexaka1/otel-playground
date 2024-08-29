# OTLP playground

https://www.youtube.com/watch?v=tctadmNTHfU
https://www.youtube.com/watch?v=HrRrJ5wTtdk

## Prerequisites

- Docker
- Docker Compose

## Running

Normal:

```bash
docker compose up -d --build
```

AoT:

```bash
docker compose -f compose.yaml -f docker-compose.prod.yaml up -d --build
```

## Dashboards

- [Aspire Dashboard](http://localhost:18888/)
- [Seq logs](http://localhost:5341/)

## Call these a couple of times

```bash
curl -X GET http://localhost:5270/todos
curl -X GET http://localhost:5270/todos/1
curl -X GET http://localhost:5270/todos/2
```
