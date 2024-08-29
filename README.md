# OTLP playground

## Prerequisites

- Docker
- Docker Compose

## Running

Normal:
```bash
docker compose up -d
```
AoT:
```bash
docker compose -f compose.yaml -f docker-compose.prod.yaml up -d
```
## Aspire Dashboard

Navigate to http://localhost:18888/

## Testing

```bash
curl -X GET http://localhost:5270/todos
```

```bash
curl -X GET http://localhost:5270/todos/1
```

```bash
curl -X GET http://localhost:5270/todos/2
```
