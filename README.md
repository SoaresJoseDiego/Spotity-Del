# SpotifyDel

App pra limpar a biblioteca de **Músicas Curtidas** do Spotify em massa, com filtros inteligentes.

Backend em **ASP.NET Core 10** com Clean Architecture, frontend em **Angular 19 standalone + Material**, persistência em **SQLite** com tokens criptografados via **ASP.NET Data Protection**.

## Funcionalidades

- Login via **OAuth 2.0** do Spotify (Authorization Code + PKCE, fluxo no backend).
- Listar todas as curtidas com paginação.
- Selecionar e remover em lote (até 50 por request, batches automáticos).
- Filtros inteligentes:
  - Curtidas anteriores a uma data.
  - Artista que aparece N+ vezes na biblioteca.
  - Excluir por gênero.
- Refresh token automático com lock por sessão.
- Retry com backoff exponencial e respeito ao `Retry-After` do Spotify.

## Setup

### 1. Registrar app no Spotify

1. Acesse [developer.spotify.com/dashboard](https://developer.spotify.com/dashboard) e crie um novo app.
2. Em **Redirect URIs**, adicione: `http://127.0.0.1:5050/api/auth/callback` (Spotify exige `127.0.0.1` literal — não aceita `localhost`).
3. Em **APIs used**, marque **Web API**.
4. Copie `Client ID` e `Client secret`.
5. Em **Settings → User Management**, adicione seu próprio email do Spotify (o mesmo de https://www.spotify.com/account/overview/). App em dev mode aceita até **5 usuários**.

   > ⚠️ **Importante**: sem este passo, o app loga normalmente e até lista suas curtidas, mas **qualquer ação de escrita (delete) ou leitura de tracks de playlist devolve 403 Forbidden** — sem mensagem de erro útil do Spotify.

### 2. Configurar secrets do backend

```powershell
cd backend/src/SpotifyDel.Api
dotnet user-secrets set "Spotify:ClientId"     "<seu_client_id>"
dotnet user-secrets set "Spotify:ClientSecret" "<seu_client_secret>"
```

### 3. Rodar backend

```powershell
# Da raiz do repo
dotnet run --project backend/src/SpotifyDel.Api
```

A API sobe em `http://127.0.0.1:5050`. A migration roda automaticamente no startup. O OpenAPI fica em `http://127.0.0.1:5050/openapi/v1.json` (modo dev).

### 4. Rodar frontend

```powershell
cd frontend/spotify-del-app
npm install   # primeira vez
npm start
```

Frontend em `http://127.0.0.1:4200` com proxy de `/api` → backend. Abra, clique **Entrar com Spotify**, autorize, e sua biblioteca aparece.

## Stack

| Camada       | Tecnologia                                  |
| ------------ | ------------------------------------------- |
| Backend      | ASP.NET Core 10, EF Core 10, SQLite         |
| Resiliência  | `Microsoft.Extensions.Http.Resilience` (Polly v8) |
| Auth         | OAuth 2.0 (PKCE) + Cookie HttpOnly assinado |
| Tokens       | Criptografados com `IDataProtector`         |
| Frontend     | Angular 19 standalone, Angular Material 19  |
| Estado       | Signals (sem RxJS Subject pra estado)       |

## Estrutura

```
SpotifyDel/
├── CLAUDE.md
├── README.md
├── .gitignore
├── backend/
│   ├── SpotifyDel.slnx
│   ├── src/
│   │   ├── SpotifyDel.Api/            # Controllers + Program.cs
│   │   ├── SpotifyDel.Application/    # Casos de uso, abstrações
│   │   ├── SpotifyDel.Domain/         # Entidades (POCO)
│   │   └── SpotifyDel.Infrastructure/ # EF Core + Spotify HttpClient
│   └── tests/
│       └── SpotifyDel.Tests/
└── frontend/
    └── spotify-del-app/               # Angular 19
        ├── proxy.conf.json
        └── src/app/
            ├── core/                  # auth, api clients, models
            └── features/              # login, tracks
```

Regra de dependência (Clean Architecture):

```
Api ──▶ Application ──▶ Domain
 │             ▲
 └──▶ Infrastructure ──┘
```

Ver [CLAUDE.md](./CLAUDE.md) pra detalhes de convenções, fluxo OAuth, schema do banco e roadmap.

## Limitações conhecidas da Spotify Web API

- **Play count por faixa não é exposto** — Spotify nunca disponibilizou.
- **Skip events não são expostos.**
- **Histórico de reprodução é limitado às últimas 50 plays** (`/me/player/recently-played`). Pra ter histórico real (filtros "não escuto há 1 ano"), é necessário coletar periodicamente e armazenar local — está no roadmap como v0.4.

## Comandos úteis

```powershell
# Compilar tudo
dotnet build backend/SpotifyDel.slnx

# Rodar testes
dotnet test backend/SpotifyDel.slnx

# Nova migration
dotnet ef migrations add <Nome> `
  --project backend/src/SpotifyDel.Infrastructure `
  --startup-project backend/src/SpotifyDel.Api `
  --output-dir Persistence/Migrations

# Apagar o banco local (recomeçar do zero)
Remove-Item backend/src/SpotifyDel.Api/spotifydel.db -ErrorAction SilentlyContinue
```

## Roadmap

- [x] **v0.1** — login, listar, filtros básicos, bulk delete
- [ ] **v0.2** — modo Tinder (swipe pra triagem com preview de 30s)
- [ ] **v0.3** — gráficos de gêneros / top artistas
- [ ] **v0.4** — `IHostedService` coletando `recently-played` a cada 30min
- [ ] **v1.0** — undo de remoção + export pra JSON
