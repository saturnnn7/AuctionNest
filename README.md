# AuctionNest

> **Real-time auction platform backend** built with ASP.NET Core 8, Clean Architecture, PostgreSQL, Redis, and SignalR.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?style=flat-square&logo=csharp)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat-square&logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=flat-square&logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-Real--time-512BD4?style=flat-square)
![CI](https://github.com/saturnnn7/AuctionNest/actions/workflows/ci.yml/badge.svg)
![Tests](https://img.shields.io/badge/Tests-44%20passing-3fb950?style=flat-square)
![Architecture](https://img.shields.io/badge/Architecture-Clean-orange?style=flat-square)

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [API Reference](#api-reference)
- [Real-time Events](#real-time-events)
- [Security](#security)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Docker Deployment](#docker-deployment)

---

## Overview

AuctionNest is a production-grade REST API for a real-time auction platform. Users can list items for auction, place competitive bids, and receive instant push notifications when they are outbid or when auctions change state — all in under 200ms via SignalR WebSockets.

The system handles concurrent bidding safely using **Redis distributed locks (Redlock)**, prevents auction sniping with an **anti-snipe extension mechanism**, guarantees reliable event delivery through the **Outbox Pattern**, and schedules auction lifecycle transitions via **Hangfire** persistent background jobs.

---

## Features

### Auction Mechanics
- **Bidding** — minimum increment enforcement, seller-cannot-bid rule, idempotent requests via `X-Idempotency-Key` header
- **Anti-snipe protection** — bids placed in the final 30 seconds extend the auction by 2 minutes (up to 5 extensions)
- **Buy It Now** — instant purchase available before the first bid is placed
- **Reserve price** — hidden floor price; outcome marked accordingly
- **Lifecycle** — `Draft → Active → Extending → Ended / Cancelled`, fully managed by Hangfire jobs

### Real-time
- Live bid updates broadcast to all auction room subscribers via **SignalR**
- Personal outbid notifications pushed to individual users
- Winner and seller notifications on auction end

### Reliability & Concurrency
- **Redlock distributed locking** on `PlaceBid` and `BuyItNow` to prevent race conditions
- **Outbox Pattern** — domain events persisted atomically with business data; a background service fans them out to SignalR and creates notifications
- **Idempotency keys** — Redis TTL-backed deduplication for bid requests

### Security
- **RS256 JWT** — asymmetric signing; private key signs, public key validates
- **Argon2id** password hashing (64 MB memory, 4 iterations, 2 threads)
- **Refresh token rotation** — single-use tokens stored in Redis with 7-day TTL
- Soft-deleted users cannot log in or interact with the system

---

## Architecture

AuctionNest follows **Clean Architecture** with strict unidirectional dependency flow:

```
Domain  ←  Application  ←  Infrastructure  ←  API
```

Each layer depends only on layers to its left. The Domain layer has zero external dependencies.

### Request Flow

```
HTTP Request
    │
    ▼
ExceptionHandlerMiddleware
    │
    ▼
Controller  ──►  MediatR  ──►  ValidationBehavior (FluentValidation)
                                      │
                                      ▼
                               Command/Query Handler
                                      │
                          ┌───────────┴───────────┐
                          ▼                       ▼
                     IUnitOfWork            Redis Cache /
                   (Repositories)         Distributed Lock
                          │
                          ▼
                      AppDbContext
                     ┌────┴────┐
                     ▼         ▼
              AuditInterceptor  OutboxInterceptor
              (CreatedAt/      (Domain Events →
               UpdatedAt)       OutboxMessage)
                                      │
                          ┌───────────┘
                          ▼
              OutboxProcessorService (5s poll)
                    ┌─────┴─────┐
                    ▼           ▼
                SignalR    Notification
                 Push        Entity
```

### Domain Events (Outbox Pattern)

Domain events are never dispatched in-process. Instead, the `OutboxInterceptor` serialises them to the `outbox_messages` table in the **same transaction** as the business data. A `BackgroundService` polls every 5 seconds, dispatches events to SignalR, creates `Notification` entities, and reschedules Hangfire jobs on auction extensions.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 + Npgsql |
| Database | PostgreSQL 16 |
| Cache / Locks | Redis 7 + StackExchange.Redis + RedLock.net |
| Real-time | ASP.NET Core SignalR |
| Background Jobs | Hangfire + Hangfire.PostgreSql |
| Mediator / CQRS | MediatR 12 |
| Validation | FluentValidation 11 |
| Auth | RS256 JWT (`System.IdentityModel.Tokens.Jwt`) |
| Password Hashing | Argon2id (`Konscious.Security.Cryptography`) |
| Containerisation | Docker + Docker Compose |
| API Docs | Swagger / Swashbuckle |
| Unit Testing | xUnit + FluentAssertions + Moq |
| API Testing | Bruno |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1 — Clone the repository

```bash
git clone https://github.com/saturnnn7/AuctionNest.git
cd AuctionNest/backend
```

### 2 — Start infrastructure (PostgreSQL + Redis)

```bash
docker compose -f docker-compose.dev.yml up -d
```

This starts:
- PostgreSQL on `localhost:5433`
- Redis on `localhost:6379`

### 3 — Generate RSA keys

```powershell
# Windows PowerShell (no Git Bash required)
./generate-keys.ps1
```

Keys are written to `src/AuctionNest.API/keys/` and are excluded from version control via `.gitignore`.

### 4 — Run the API

```bash
dotnet run --project src/AuctionNest.API
```

The API starts on `http://localhost:5165`. EF Core migrations are applied automatically on startup.

### 5 — Explore

| URL | Description |
|---|---|
| `http://localhost:5165/swagger` | Interactive API documentation |
| `http://localhost:5165/hangfire` | Background job dashboard |
| `http://localhost:5165/signalr-test.html` | Real-time SignalR test page |

### 6 — Seed a category (required to create auctions)

```sql
INSERT INTO categories (id, name, slug, created_at, updated_at)
VALUES ('a1b2c3d4-0000-0000-0000-000000000001', 'Electronics', 'electronics', NOW(), NOW());
```

---

## API Reference

All endpoints return a consistent error shape:

```json
{
  "type": "NotFound",
  "title": "Auction.NotFound",
  "detail": "Auction was not found.",
  "status": 404
}
```

### Auth

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | — | Register a new user |
| `POST` | `/api/auth/login` | — | Login, receive JWT pair |
| `POST` | `/api/auth/refresh` | — | Rotate refresh token |

### Auctions

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/auctions` | — | Paged list with filters |
| `GET` | `/api/auctions/{id}` | — | Full details with recent bids |
| `POST` | `/api/auctions` | ✅ | Create auction |
| `POST` | `/api/auctions/{id}/bids` | ✅ | Place bid |
| `POST` | `/api/auctions/{id}/buy-it-now` | ✅ | Instant purchase |
| `DELETE` | `/api/auctions/{id}/cancel` | ✅ | Cancel auction (seller only) |

**Query parameters for `GET /api/auctions`:**

| Parameter | Type | Description |
|---|---|---|
| `search` | `string` | Full-text search on title and description |
| `categoryId` | `guid` | Filter by category |
| `status` | `string` | `Draft`, `Active`, `Extending`, `Ended`, `Cancelled` |
| `minPrice` | `decimal` | Minimum current price |
| `maxPrice` | `decimal` | Maximum current price |
| `page` | `int` | Page number (default: 1) |
| `pageSize` | `int` | Items per page (default: 20) |
| `sortBy` | `string` | `endsAt` (default) or `price` |
| `sortDescending` | `bool` | Sort direction (default: false) |

**Idempotent bidding** — include `X-Idempotency-Key: <uuid>` header on `POST /bids` to prevent duplicate bids on network retry.

### WatchList

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/watchlist` | ✅ | Get watched auctions |
| `POST` | `/api/watchlist/{auctionId}` | ✅ | Watch an auction |
| `DELETE` | `/api/watchlist/{auctionId}` | ✅ | Unwatch an auction |

### Notifications

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/notifications` | ✅ | Paged notification list |
| `GET` | `/api/notifications/unread-count` | ✅ | Unread notification count |
| `PATCH` | `/api/notifications/{id}/read` | ✅ | Mark single as read |
| `PATCH` | `/api/notifications/read-all` | ✅ | Mark all as read |

### Users & Categories

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/users/me` | ✅ | Get own profile |
| `PATCH` | `/api/users/me/display-name` | ✅ | Update display name |
| `GET` | `/api/categories` | — | List all categories |

---

## Real-time Events

Connect to the SignalR hub at `/hubs/auction`.

To receive personal notifications (outbid alerts, auction won), authenticate via query string:
```
ws://localhost:5165/hubs/auction?access_token=<jwt>
```

### Join / Leave an auction room

```javascript
await connection.invoke('JoinAuction', auctionId);
await connection.invoke('LeaveAuction', auctionId);
```

### Server-pushed events

| Event | Trigger | Payload |
|---|---|---|
| `BidPlaced` | New bid placed | `auctionId`, `bidderId`, `amount`, `newCurrentPrice`, `occurredAt` |
| `AuctionExtended` | Anti-snipe triggered | `auctionId`, `newEndsAt`, `extensionCount` |
| `AuctionEnded` | Auction finished | `auctionId`, `winnerId`, `winningAmount`, `isReserveMet` |
| `AuctionStarted` | Auction activated | `auctionId`, `occurredAt` |
| `AuctionCancelled` | Auction cancelled | `auctionId`, `occurredAt` |
| `NewNotification` | Personal alert | `id`, `title`, `message`, `type` |

**Observed latency: ~100ms** (Outbox polling interval: 5 seconds, typically caught in first poll tick).

---

## Security

### JWT

Tokens are signed with **RSA-2048 (RS256)**. The private key signs access tokens; only the public key is needed for validation, enabling future microservice decomposition without sharing secrets.

- Access token expiry: **15 minutes**
- Refresh token expiry: **7 days** (stored in Redis, rotated on every use)

### Password hashing

Argon2id with the following parameters:

| Parameter | Value |
|---|---|
| Memory | 64 MB |
| Iterations | 4 |
| Parallelism | 2 |
| Hash size | 32 bytes |
| Salt size | 16 bytes |

### Concurrency

`PlaceBid` and `BuyItNow` acquire a **Redlock** on `auction:{id}` before loading the auction aggregate. This prevents race conditions where two concurrent bids could both read the same `CurrentPrice` and both pass the minimum bid check.

---

## Testing

### Unit Tests

```bash
dotnet test tests/AuctionNest.UnitTests --verbosity normal
```

```
Total tests: 44  |  Passed: 44  |  Failed: 0
```

Covers the `Auction` aggregate root across all domain scenarios:

| Area | Tests |
|---|---|
| Activate | Draft → Active, invalid transitions |
| PlaceBid | Happy path, bid too low, seller restriction, previous winner tracking |
| Anti-snipe | Extension triggered, not triggered, max extensions limit |
| BuyItNow | Happy path, after bid placed, seller restriction, events |
| Cancel | Active/Draft, cannot cancel Ended/Cancelled |
| End | No winner, winner, reserve met/not met, cannot end twice |
| Computed properties | `IsReserveMet`, `IsBuyItNowAvailable` |

### API Tests (Bruno)

A complete Bruno collection is located at `backend/bruno/`.

Import the `bruno/` folder in the Bruno desktop app, select the **Local** environment, and run requests in sequence:

1. `Auth/Register` — creates user and saves JWT to environment
2. `Categories/Get Categories` — saves `categoryId`
3. `Auctions/Create Auction` — creates auction with dynamic `startsAt` (+10 seconds), saves `auctionId`
4. Wait ~15 seconds for Hangfire to activate the auction
5. All remaining endpoints

### SignalR Manual Test

Open `http://localhost:5165/signalr-test.html`, connect, join an auction room, and place a bid via Swagger or Bruno. Expect `BidPlaced` in the event log within ~100ms.

---

## Project Structure

```
backend/
├── src/
│   ├── AuctionNest.Domain/               # Enterprise business rules
│   │   ├── Common/                       # Entity, Result<T>, Error, IDomainEvent
│   │   ├── Entities/                     # Auction, Bid, User, Category, ...
│   │   ├── Enums/                        # AuctionStatus, NotificationType, UserRole
│   │   ├── Errors/                       # Typed error constants per aggregate
│   │   └── Events/                       # BidPlacedEvent, AuctionEndedEvent, ...
│   │
│   ├── AuctionNest.Application/          # Application business rules
│   │   ├── Common/
│   │   │   ├── Behaviors/               # ValidationBehavior (MediatR pipeline)
│   │   │   ├── Exceptions/              # ValidationException
│   │   │   ├── Interfaces/              # IUnitOfWork, IRepository<T>, ICurrentUserService
│   │   │   │   ├── Repositories/        # IAuctionRepository, IBidRepository, ...
│   │   │   │   └── Services/            # IJwtService, ICacheService, IJobScheduler, ...
│   │   │   └── Models/                  # PagedResponse<T>, AuctionFilterParams
│   │   └── Features/                    # CQRS — one folder per feature
│   │       ├── Auctions/Commands/       # CreateAuction, PlaceBid, BuyItNow, CancelAuction
│   │       ├── Auctions/Queries/        # GetAuctions, GetAuctionById
│   │       ├── Auth/Commands/           # Register, Login, RefreshToken
│   │       ├── Notifications/           # GetMyNotifications, MarkAsRead, ...
│   │       ├── Users/                   # GetMyProfile, UpdateDisplayName
│   │       └── WatchList/               # AddToWatchList, RemoveFromWatchList, ...
│   │
│   ├── AuctionNest.Infrastructure/       # Frameworks & drivers
│   │   ├── BackgroundJobs/              # ActivateAuctionJob, EndAuctionJob (Hangfire)
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/          # EF Core Fluent API per entity
│   │   │   ├── Interceptors/            # AuditInterceptor, OutboxInterceptor
│   │   │   ├── Repositories/            # Concrete repository implementations
│   │   │   └── UnitOfWork.cs
│   │   └── Services/                    # JwtService, PasswordHasher, CacheService, ...
│   │
│   └── AuctionNest.API/                  # Delivery layer
│       ├── BackgroundServices/          # OutboxProcessorService
│       ├── Common/                      # BaseController, ExceptionHandlerMiddleware
│       ├── Controllers/                 # AuctionsController, AuthController, ...
│       ├── Hubs/                        # AuctionHub (SignalR)
│       ├── wwwroot/                     # signalr-test.html
│       └── Program.cs
│
├── tests/
│   ├── AuctionNest.UnitTests/
│   │   └── Domain/
│   │       ├── Entities/                # AuctionTests.cs (44 tests)
│   │       └── Helpers/                 # AuctionFactory.cs
│   └── AuctionNest.IntegrationTests/    # Placeholder (Testcontainers)
│
├── bruno/                                # Bruno API test collection
│   ├── environments/Local.bru
│   ├── Auth/
│   ├── Auctions/
│   ├── Notifications/
│   ├── Users/
│   ├── WatchList/
│   └── Categories/
│
├── docker-compose.yml                   # Production compose (API + DB + Redis)
├── docker-compose.dev.yml               # Dev infrastructure only (DB + Redis)
├── Dockerfile
└── generate-keys.ps1                    # RSA key generation script
```

---

## Configuration

All settings are in `appsettings.json` / environment variables.

| Key | Default | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `localhost:5433` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | `localhost:6379` | Redis connection string |
| `Jwt__Issuer` | `auctionnest-api` | JWT issuer claim |
| `Jwt__Audience` | `auctionnest-client` | JWT audience claim |
| `Jwt__PrivateKeyPath` | `keys/private.pem` | Path to RSA private key |
| `Jwt__PublicKeyPath` | `keys/public.pem` | Path to RSA public key |
| `Jwt__AccessTokenExpiryMinutes` | `15` | Access token lifetime |
| `Jwt__RefreshTokenExpiryDays` | `7` | Refresh token lifetime |

---

## Docker Deployment

### Full stack (API + PostgreSQL + Redis)

```bash
# Create a .env file with your secrets
echo "POSTGRES_PASSWORD=your_secure_password" > .env

# Build and start
docker compose up -d --build
```

The API will be available on `http://localhost:8080`.

> **Note:** RSA keys must exist at `/app/keys/` inside the container. Mount them as a Docker secret or volume — never commit them to source control.

### EF Core Migrations

Migrations are applied automatically on startup via `DatabaseMigrator`. To generate a new migration manually:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/AuctionNest.Infrastructure \
  --startup-project src/AuctionNest.API \
  --output-dir Persistence/Migrations
```

---

## Author

**Kyrylo Soprykin** — Junior .NET Backend Developer

[![LinkedIn](https://img.shields.io/badge/LinkedIn-Connect-0A66C2?style=flat-square&logo=linkedin)](https://linkedin.com/in/kyrylo-soprykin)
[![GitHub](https://img.shields.io/badge/GitHub-saturnnn7-181717?style=flat-square&logo=github)](https://github.com/saturnnn7)
