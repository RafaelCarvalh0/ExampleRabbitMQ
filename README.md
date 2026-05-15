# 🐇 ExampleRabbitMQ

Sistema de mensageria assíncrona com **RabbitMQ** em **.NET 10 (C#)**, simulando um fluxo completo de pedidos com Producer, Worker Service, retentativas automáticas (Retry), Dead Letter Queue (DLQ), persistência no **MongoDB** e dashboard em tempo real com **SignalR**.

---

## 📌 Visão Geral

Este projeto demonstra uma arquitetura **Event-Driven** com múltiplos serviços desacoplados. Mensagens publicadas pelo Producer são consumidas pelo Worker Service, que valida, persiste no MongoDB e publica eventos de resultado. A aplicação web consome esses eventos via RabbitMQ e os exibe em tempo real no browser via WebSocket (SignalR), sem necessidade de refresh.

Mensagens que falham no processamento passam por um ciclo de **retry com delay (TTL)** antes de serem descartadas na **DLQ**, evitando perda silenciosa de dados e sobrecarga no sistema.

---

## 🏛️ Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                        RabbitMQ (Docker)                        │
│                                                                 │
│  pedido.principal ──► pedido.criados ──► pedido.retry (TTL)    │
│  pedido.processado ──► pedido.processado (Fanout)               │
│  pedido.dlx ──────────► pedido.dlq                              │
└─────────────────────────────────────────────────────────────────┘
         ▲                      │                      │
         │                      ▼                      ▼
  ┌──────────────┐    ┌──────────────────┐   ┌─────────────────────┐
  │   Producer   │    │  Worker Service  │   │  ASP.NET MVC        │
  │ (Console App)│    │ (Background Svc) │   │  + SignalR          │
  └──────────────┘    └────────┬─────────┘   └──────────┬──────────┘
                               │                        │
                               ▼                        ▼
                      ┌─────────────────┐      ┌───────────────────┐
                      │    MongoDB      │◄─────│  Browser (Grid    │
                      │  (WorkerDb)     │      │  em tempo real)   │
                      └─────────────────┘      └───────────────────┘
```

---

## 🔄 Fluxo de Mensagens

```
Producer
   │
   ▼
pedido.principal (Direct Exchange)
   │
   ▼
pedido.criados (Fila Principal)
   │
   ├── ✅ Sucesso
   │       │── Persiste no MongoDB (status: Processado)
   │       │── Publica em pedido.processado
   │       └── BasicAck → mensagem removida da fila
   │
   ├── 🔁 Erro temporário
   │       │── Incrementa x-retry-count no header
   │       ▼
   │   pedido.retry (TTL configurado)
   │       │
   │       └── ⏱️ Após delay → pedido.principal → pedido.criados (nova tentativa)
   │                               │
   │                               └── ❌ Esgotou tentativas
   │                                       │── Persiste no MongoDB (status: Falhou)
   │                                       │── Publica em pedido.processado
   │                                       └── Nack → DLX → pedido.dlq
   │
   └── ❌ Erro definitivo (dado inválido)
           │── Persiste no MongoDB (status: Falhou + motivo)
           │── Publica em pedido.processado
           └── Nack → DLX → pedido.dlq

pedido.processado (Fanout Exchange)
   │
   └── ASP.NET MVC Consumer
           └── SignalR → Browser (grid atualiza em tempo real)
```

---

## 🏗️ Exchanges e Filas

| Nome | Tipo | Papel |
|---|---|---|
| `pedido.principal` | Direct | Exchange principal — recebe pedidos do Producer |
| `pedido.retry` | Direct | Exchange de retry — recebe mensagens para reprocessar com delay |
| `pedido.dlx` | Direct | Dead Letter Exchange — recebe mensagens descartadas |
| `pedido.processado` | Fanout | Exchange de eventos — notifica resultado do processamento |
| `pedido.criados` | Fila | Fila principal de processamento |
| `pedido.retry` | Fila (TTL) | Fila de espera para retry — devolve mensagens após o delay |
| `pedido.dlq` | Fila | Dead Letter Queue — armazena mensagens que falharam definitivamente |
| `pedido.processado` | Fila | Fila consumida pelo MVC para atualizar o dashboard |

---

## ⚙️ Como Funciona o Retry

1. O Worker recebe uma mensagem de `pedido.criados`
2. Se o processamento falhar com erro temporário, incrementa o header `x-retry-count`
3. Se ainda há tentativas disponíveis → publica na exchange `pedido.retry`
4. A mensagem fica em `pedido.retry` por um tempo (TTL configurável em `Retries.RetryDelayMs`)
5. Após o delay, o RabbitMQ redireciona automaticamente para `pedido.principal` → `pedido.criados`
6. Se o número máximo de tentativas for atingido → persiste como `Falhou` → `pedido.dlx` → `pedido.dlq`

```csharp
// Controle de tentativas via header customizado
if (retryCount < Retries.MaxRetryAttempts - 1)
    // → Retry Exchange → TTL delay → nova tentativa
else
    // → Persiste Falhou → DLX → DLQ
```

---

## 🗂️ Estrutura do Projeto

```
ExampleRabbitMQ/
│
├── RabbitMQ.Application/        # ASP.NET MVC — Dashboard web
│   ├── Controllers/
│   │   └── PedidoController.cs  # GET /api/pedidos · DELETE /api/pedidos/{id}
│   ├── Services/
│   │   ├── Handlers/
│   │   │   └── PedidoApplicationHandler.cs  # Recebe evento e empurra pro SignalR
│   │   ├── Workers/
│   │   │   └── PedidoConsumerWorker.cs      # BackgroundService — consumer da fila processado
│   │   └── PedidoHub.cs                     # SignalR Hub — WebSocket com o browser
│   └── Views/
│       └── Pedido/
│           ├── Index.cshtml      # Monitor em tempo real (só SignalR)
│           └── Historico.cshtml  # Histórico do MongoDB + SignalR + DELETE
│
├── RabbitMQ.Consumer/           # Worker Service — processamento de pedidos
│   ├── Handlers/
│   │   └── PedidoHandler.cs     # Valida, persiste, publica evento, gerencia retry/DLQ
│   └── Worker.cs                # BackgroundService — consumer da fila principal
│
├── RabbitMQ.Infrastructure/     # Acesso a dados — MongoDB
│   └── Repositories/
│       ├── IPedidoRepository.cs # Interface: Save, Exists, GetAll, Delete
│       └── PedidoRepository.cs  # Implementação com MongoDB.Driver
│
├── RabbitMQ.Models/             # Modelos compartilhados entre os projetos
│   └── Models/
│       └── Pedido/
│           ├── Enums/
│           │   └── StatusPedido.cs          # Processado | Falhou
│           ├── PedidoRequest.cs             # Modelo de entrada (Producer)
│           ├── PedidoItemRequest.cs         # Item do pedido
│           └── PedidoProcessadoEntity.cs    # Documento MongoDB + evento RabbitMQ
│
├── RabbitMQ.Producer/           # Console App — publica pedidos no RabbitMQ
│   └── Handlers/
│       ├── PedidoFakeFactory.cs # Gerador de pedidos fake para testes
│       └── PedidoPublisher.cs   # Publica na exchange principal
│
├── RabbitMQ.Shared/             # Configurações e constantes compartilhadas
│   ├── Infrastructure/
│   │   ├── RabbitMqConnectionFactory.cs
│   │   └── RabbitMqQueueSetup.cs  # Declara todas as exchanges, filas e binds
│   └── Messaging/
│       ├── Exchanges.cs           # Nomes das exchanges
│       ├── Queues.cs              # Nomes das filas
│       ├── RoutingKeys.cs         # Routing keys
│       ├── Retries.cs             # MaxRetryAttempts, RetryDelayMs
│       ├── RabbitMqSettings.cs    # Host, Port, User, Password
│       └── MongoDbSettings.cs     # ConnectionString, DatabaseName, CollectionName
│
└── docker-compose.yaml           # RabbitMQ + MongoDB
```

---

## 🖥️ Dashboard Web

A aplicação MVC expõe duas views:

**Monitor em Tempo Real** (`/Pedido/Index`)
- Conexão WebSocket via SignalR
- Exibe pedidos conforme chegam da exchange `pedido.processado`
- Cards de métricas: total, processados, falhos, volume financeiro
- Filtros por status e animação de entrada

**Histórico de Pedidos** (`/Pedido/Historico`)
- Carga inicial via `GET /api/pedidos` direto no MongoDB
- SignalR escuta `NovoPedido` — novos pedidos aparecem no topo em tempo real
- SignalR escuta `PedidoRemovido` — linha some em todos os browsers conectados
- Busca por email ou ID
- Exclusão com modal de confirmação → `DELETE /api/pedidos/{id}`

---

## ▶️ Como Executar

### 1. Subir a infraestrutura via Docker

```bash
docker-compose up -d
```

| Serviço | URL | Credenciais |
|---|---|---|
| RabbitMQ Management | http://localhost:15672 | admin / admin |
| MongoDB | mongodb://localhost:27017 | — |

### 2. Rodar o Worker Service

```bash
cd RabbitMQ.Consumer
dotnet run
```

### 3. Rodar a aplicação web

```bash
cd RabbitMQ.Application
dotnet run
```

> Acesse o dashboard em `https://localhost:{porta}/Pedido`

### 4. Rodar o Producer

```bash
cd RabbitMQ.Producer
dotnet run
```

> O Producer pergunta quantos pedidos enviar e se deve simular erros (valor negativo → DLQ).

---

## 🛣️ Roadmap

- [x] Producer (Console App)
- [x] Worker Service com Retry e DLQ
- [x] Configuração de Exchanges, Filas e Binds via `RabbitMqQueueSetup`
- [x] Persistência no MongoDB com idempotência
- [x] Exchange `pedido.processado` (Fanout) para notificação de resultado
- [x] ASP.NET MVC com SignalR — dashboard em tempo real
- [x] Histórico com carga do MongoDB + delete em tempo real
- [x] Docker Compose com RabbitMQ + MongoDB
- [ ] Testes de integração
- [ ] Observabilidade (logs estruturados com Serilog)
- [ ] Instalação como Windows Service (`sc.exe`)

---

## 🚀 Tecnologias

| Tecnologia | Uso |
|---|---|
| [.NET 10 / C#](https://dotnet.microsoft.com/) | Plataforma principal |
| [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client) | Mensageria assíncrona |
| [MongoDB.Driver](https://www.nuget.org/packages/MongoDB.Driver) | Persistência de documentos |
| [ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc) | Dashboard web |
| [SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction) | WebSocket — atualizações em tempo real |
| [Bootstrap 5](https://getbootstrap.com/) | Interface do dashboard |
| [Docker + Docker Compose](https://docs.docker.com/compose/) | Infraestrutura local |
