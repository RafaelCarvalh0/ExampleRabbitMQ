# 🐇 ExampleRabbitMQ

Sistema de mensageria assíncrona com **RabbitMQ** em **.NET 10 (C#)**, simulando um fluxo de pedidos com Producer, Consumer, retentativas automáticas (Retry) e Dead Letter Queue (DLQ).

> ⚠️ **Projeto em desenvolvimento** — Worker Service em construção.

---

## 📌 Visão Geral

Este projeto demonstra um padrão robusto de mensageria assíncrona, onde mensagens que falham no processamento passam por um ciclo de **retry com delay** antes de serem descartadas na **DLQ**, evitando perda silenciosa de dados e sobrecarga no sistema.

---

## 🔄 Fluxo de Mensagens

```
Producer
   │
   ▼
pedido.exchange (Direct)
   │
   ▼
pedido.criados (Fila Principal)
   │
   ├── ✅ Sucesso              → Ack (mensagem removida da fila)
   │
   ├── 🔁 Erro temporário      → Publish no Retry Exchange
   │       │
   │       ▼
   │   pedido.retry (TTL configurado)
   │       │
   │       └── ⏱️ Após delay   → pedido.exchange → pedido.criados (nova tentativa)
   │                               │
   │                               └── ❌ Esgotou tentativas → DLX → pedido.dlq
   │
   └── ❌ Erro definitivo      → Nack → DLX → pedido.dlq
```

> O consumer controla o número de tentativas verificando o header `x-death` de cada mensagem.

---

## 🏗️ Exchanges e Filas

| Nome | Tipo | Papel |
|---|---|---|
| `pedido.exchange` | Direct | Exchange principal — recebe pedidos do Producer |
| `pedido.retry.exchange` | Direct | Exchange de retry — recebe mensagens para reprocessar com delay |
| `pedido.dlx.exchange` | Direct | Dead Letter Exchange — recebe mensagens descartadas |
| `pedido.criados` | Fila | Fila principal de processamento |
| `pedido.retry` | Fila (TTL) | Fila de espera para retry — devolve mensagens após o delay |
| `pedido.dlq` | Fila | Dead Letter Queue — armazena mensagens que falharam definitivamente |

---

## ⚙️ Como Funciona o Retry

1. O Consumer recebe uma mensagem de `pedido.criados`
2. Se o processamento falhar, ele verifica o header `x-death` para saber quantas tentativas já ocorreram
3. Se ainda há tentativas disponíveis → publica na `pedido.retry.exchange`
4. A mensagem fica em `pedido.retry` por um tempo (TTL configurável)
5. Após o delay, o RabbitMQ redireciona automaticamente para `pedido.exchange` → `pedido.criados`
6. Se o número máximo de tentativas for atingido → publica na `pedido.dlx.exchange` → `pedido.dlq`

```csharp
// Exemplo de verificação de tentativas no Consumer
var deathCount = GetDeathCount(basicProperties); // lê x-death count

if (deathCount >= Retries.MaxAttempts)
    // → DLX → DLQ
else
    // → Retry Exchange → delay → nova tentativa
```

---

## 🗂️ Estrutura do Projeto

```
ExampleRabbitMQ/
├── RabbitMQ.Shared/        # Infraestrutura compartilhada
│   ├── Messaging/          # Constantes: Exchanges, Queues, RoutingKeys
│   └── Infrastructure/     # ConnectionFactory, QueueSetup (declaração de filas e binds)
│
├── RabbitMQ.Models/        # Modelos de domínio
│   └── Pedido, Item, etc.
│
├── RabbitMQ.Producer/      # Console App — publica mensagens no RabbitMQ
├── RabbitMQ.Consumer/      # Console App — consome e processa mensagens com retry/DLQ
└── docker-compose.yaml     # RabbitMQ + Management UI
```

---

## ▶️ Como Executar

### 1. Subir o RabbitMQ via Docker

```bash
docker-compose up -d
```

> Painel de gerenciamento disponível em `http://localhost:15672`
> Login: `admin` / Senha: `admin`

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

> Certifique-se de que o RabbitMQ já está de pé antes de rodar Producer ou Consumer.

---

## 🛣️ Roadmap

- [x] Producer (Console App)
- [x] Consumer com Retry e DLQ (Console App)
- [x] Configuração de Exchanges, Filas e Binds via `RabbitMqQueueSetup`
- [x] Docker Compose com RabbitMQ
- [ ] Worker Service para substituir o Consumer Console App
- [ ] Testes de integração
- [ ] Observabilidade (logs estruturados / métricas)

---

## 🚀 Tecnologias

- [.NET 10 / C#](https://dotnet.microsoft.com/)
- [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client)
- [Docker + Docker Compose](https://docs.docker.com/compose/)
