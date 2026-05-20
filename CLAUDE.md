# SpotifyDel

App pessoal pra limpar a biblioteca de "Músicas Curtidas" do Spotify em massa, com filtros inteligentes. Também serve como peça de portfólio do dev.

## Stack

| Camada       | Tecnologia                                  |
| ------------ | ------------------------------------------- |
| Backend      | ASP.NET Core 10 (Web API)                   |
| Arquitetura  | Clean Architecture (Api/App/Domain/Infra)   |
| Persistência | EF Core + SQLite                            |
| Auth         | Spotify OAuth 2.0 (Authorization Code+PKCE) |
| Frontend     | Angular 19 standalone + Angular Material    |
| HTTP cliente | HttpClient tipado + Polly (retry/backoff)   |

## Estrutura

```
SpotifyDel/
├── backend/
│   ├── SpotifyDel.sln
│   ├── src/
│   │   ├── SpotifyDel.Api/            # Web API, controllers, Program.cs
│   │   ├── SpotifyDel.Application/    # Casos de uso, DTOs, abstrações
│   │   ├── SpotifyDel.Domain/         # Entidades, sem dependências externas
│   │   └── SpotifyDel.Infrastructure/ # Spotify client, EF Core, repositórios
│   └── tests/
│       └── SpotifyDel.Tests/
└── frontend/
    └── spotify-del-app/               # Angular 19 standalone
```

**Regra de dependência (Clean Architecture):**

```
Api ──▶ Application ──▶ Domain
 │            ▲
 └──▶ Infrastructure ──┘
```

Domain não depende de nada. Application só depende de Domain. Infrastructure e Api referenciam Application (e Domain via transitividade). **Nunca** referenciar Infrastructure a partir de Application.

## Migração Feb/2026 da Spotify Web API

Em fevereiro de 2026 o Spotify renomeou silenciosamente vários endpoints. **Os antigos devolvem `403 Forbidden` sem mensagem** (não 410), o que faz parecer bloqueio de permissão. Endpoints em uso pelo SpotifyDel:

| Antigo | Novo |
| --- | --- |
| `DELETE /me/tracks` body `{ids}` | `DELETE /me/library?uris=spotify:track:A,spotify:track:B` (**query string**, não body — a doc da migração engana; max **40**) |
| `PUT /me/tracks` | `PUT /me/library?uris=...` (mesmo padrão de query string) |
| `GET /me/tracks/contains?ids=` | `GET /me/library/contains?uris=spotify:track:...` |
| `GET /playlists/{id}/tracks` | `GET /playlists/{id}/items` — wrapper agora `{added_at, item:{track-fields}}` (era `track`) |
| `DELETE /playlists/{id}/tracks` body `{tracks:[{uri}]}` | `DELETE /playlists/{id}/items` body `{items:[{uri}]}` |

Permanecem: `GET /me/tracks`, `GET /me/playlists`, `GET /me`, `GET /me/top/*`, `GET /artists/{id}`.

Guia oficial: https://developer.spotify.com/documentation/web-api/tutorials/february-2026-migration-guide

## Limitações da Spotify Web API (importantes)

- **Play count por faixa**: não existe. Spotify nunca expôs.
- **Skip events**: não existe.
- **Recently played**: `/me/player/recently-played` retorna só **as últimas 50 reproduções**. Pra ter histórico real é preciso coletar periodicamente e armazenar local.
- **Rate limit**: ~30s rolling window, retorna `429` com `Retry-After` — respeitar o header.
- **Quota**: app em dev mode aceita **5 usuários** listados manualmente no dashboard (Spotify reduziu de 25 pra 5 em 2025). Pra este projeto (uso pessoal) basta — basta adicionar o próprio email em Settings → User Management. Sem isso, escrita e leitura profunda devolvem **403 Forbidden mudo** mesmo com scopes corretos.
- **Tokens**: access token = 1h; refresh token não expira até revogação. Backend renova transparentemente.

Por causa disso, **filtros que dependem de histórico ("não escuto há 1 ano", "frequência")** só ficam funcionais depois de meses com o coletor rodando. MVP foca no que é viável imediatamente:

- Filtrar por `added_at` (data em que foi curtida)
- Artistas/álbuns repetidos nas curtidas
- Gêneros (via `/artists/{id}`)
- Audio features (energy, valence, danceability, tempo)
- Modo Tinder pra triagem manual rápida

## Comandos comuns

### Backend

```powershell
# Restaurar e compilar
dotnet restore backend/SpotifyDel.sln
dotnet build   backend/SpotifyDel.sln

# Rodar API (porta padrão 5050)
dotnet run --project backend/src/SpotifyDel.Api

# Migrations EF Core
dotnet ef migrations add <Nome> --project backend/src/SpotifyDel.Infrastructure --startup-project backend/src/SpotifyDel.Api
dotnet ef database update             --project backend/src/SpotifyDel.Infrastructure --startup-project backend/src/SpotifyDel.Api

# Testes
dotnet test backend/SpotifyDel.sln

# Secrets (Spotify credentials) — ver README pra detalhes
dotnet user-secrets set "Spotify:ClientId"     "<seu_client_id>"     --project backend/src/SpotifyDel.Api
dotnet user-secrets set "Spotify:ClientSecret" "<seu_client_secret>" --project backend/src/SpotifyDel.Api
```

### Frontend

```powershell
cd frontend/spotify-del-app
npm install
npm start          # ng serve, http://127.0.0.1:4200
npm run build
npm test
```

## Convenções de código

### C# (.NET 10)

- `nullable enable` em todos os csproj.
- `ImplicitUsings` ativado.
- Records pra DTOs imutáveis.
- `async`/`await` em todo I/O. `CancellationToken` em todo método público async.
- Endpoints retornam `Results<Ok<T>, ProblemHttpResult, ...>` (typed results) sempre que possível.
- Logging via `ILogger<T>`. Sem `Console.WriteLine`.
- DI por construtor; sem `IServiceLocator`.
- Abstrações em Application, implementações em Infrastructure. Resolver em `Program.cs`.

### Angular

- **Standalone components** (sem NgModules).
- **Signals** pra estado local; RxJS apenas pra streams (HttpClient).
- `inject()` em vez de constructor injection.
- `OnPush` change detection por padrão.
- Estilo: SCSS, BEM-like.
- Estrutura: `core/` (singletons), `features/` (rotas lazy), `shared/` (componentes reutilizáveis).

### Geral

- Mensagens de commit em inglês, modo imperativo: "Add liked tracks endpoint", "Fix token refresh race".
- Sem comentários óbvios. Comentar só o **porquê** quando não-óbvio.
- Sem dead code, sem `TODO` solto — abrir issue ou apagar.

## Fluxo OAuth

1. Frontend chama `GET /api/auth/login` → backend gera `state` + `code_verifier`, salva em cookie HttpOnly, redireciona pra `https://accounts.spotify.com/authorize` com `code_challenge`.
2. Spotify redireciona pra `GET /api/auth/callback?code=...&state=...` → backend valida `state`, troca `code` por tokens, persiste em SQLite, cria sessão (cookie HttpOnly assinado), redireciona pro frontend.
3. Requests subsequentes do frontend levam o cookie de sessão. Backend resolve `UserSession` → recupera tokens → chama Spotify API.
4. Se access token expirou, `SpotifyAuthenticationHandler` (DelegatingHandler) renova com refresh token transparentemente.

Por que cookie HttpOnly em vez de devolver token pro frontend: evita XSS extrair tokens da Spotify. Backend é o único que toca em access/refresh tokens.

## Endpoints (MVP)

| Método | Rota                                  | Descrição                              |
| ------ | ------------------------------------- | -------------------------------------- |
| GET    | `/api/auth/login`                     | Inicia fluxo OAuth                     |
| GET    | `/api/auth/callback`                  | Callback do Spotify                    |
| GET    | `/api/auth/me`                        | Perfil do usuário logado               |
| POST   | `/api/auth/logout`                    | Encerra sessão                         |
| GET    | `/api/tracks/liked?limit=&offset=`    | Curtidas paginadas                     |
| DELETE | `/api/tracks/liked`                   | Body: `{ ids: string[] }` — bulk delete|
| POST   | `/api/tracks/filter`                  | Aplica filtros e retorna IDs candidatos|
| GET    | `/api/stats/genres`                   | Distribuição de gêneros                |
| GET    | `/api/stats/artists/top`              | Top artistas                           |

## Schema do banco (SQLite)

- `UserSessions` (id, spotify_user_id, display_name, created_at, last_seen_at)
- `SpotifyTokens` (session_id, access_token_encrypted, refresh_token_encrypted, expires_at)
- `RecentlyPlayedSnapshots` (id, session_id, track_id, played_at, source) — pra coletor futuro de histórico
- `TrackCache` (id, name, artists_json, album_json, duration_ms, fetched_at) — cache curto pra não martelar API

Tokens armazenados criptografados com Data Protection API do ASP.NET.

## Roadmap

**v0.1 (MVP)** — login, listar curtidas, filtros básicos, bulk delete.
**v0.2** — modo Tinder com preview de 30s.
**v0.3** — gráficos (gêneros, top artistas) com ng2-charts.
**v0.4** — background job (`IHostedService`) coleta `recently-played` a cada 30min, ativando filtros baseados em histórico.
**v1.0** — undo (toast com "desfazer" pós-delete), exportar lista removida pra JSON.

## Não fazer

- Não tentar arquitetar pra multi-tenant. Modo dev do Spotify limita a 25 users — projeto é single-user.
- Não armazenar tokens em plain text. Sempre via Data Protection.
- Não fazer chamadas Spotify direto do frontend. Tudo via backend (motivos de segurança e de cache).
- Não usar Entity Framework no Domain. Domain é POCO.
- Não criar abstrações "por precaução". Adicionar interface só quando há segunda implementação ou necessidade de mock em teste.
