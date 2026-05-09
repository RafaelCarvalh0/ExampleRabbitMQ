# 🐇 ExampleRabbitMQ

Sistema de mensageria assíncrona com **RabbitMQ** em **.NET 10 (C#)**, simulando um fluxo de pedidos com Producer, Consumer e Dead Letter Queue (DLQ).

---

## 🔄 Fluxo de Mensagens

```
Producer
   │
   ▼
pedido.exchange (Direct)
   │
   ▼
pedido.criados
   ├── ✅ Sucesso         → Ack
   ├── ⚠️ Erro temporário → Nack + requeue (retry)
   └── ❌ Erro definitivo → Nack → pedido.dlx.exchange → pedido.dlq
```

---

## 🏗️ Estrutura

```
├── RabbitMQ.Shared/        # Exchanges, Filas, RoutingKeys, ConnectionFactory, QueueSetup
├── RabbitMQ.Models/        # Modelos de domínio (Pedido, Item)
├── RabbitMQ.Producer/      # Publicação de mensagens
└── RabbitMQ.Consumer/      # Consumo e tratamento de erros
```

---

## ▶️ Como Executar

### 1. Subir o RabbitMQ

```bash
docker-compose up -d
```

> Painel disponível em `http://localhost:15672` — login: `admin` / `admin`

### 2. Rodar o Producer

```bash
cd RabbitMQ.Producer
dotnet run
```

### 3. Rodar o Consumer (outro terminal)

```bash
cd RabbitMQ.Consumer
dotnet run
```

---

## 🚀 Tecnologias

- .NET 10 / C#
- RabbitMQ.Client
- Docker + Docker Compose
