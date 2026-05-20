# SpotifyDel

App pra limpar e analisar sua biblioteca do Spotify em massa, com filtros inteligentes, dashboard estilo Wrapped, modo Tinder pra triagem rápida e exportação de imagem compartilhável.

> 🎵 Backend em **ASP.NET Core 10** com Clean Architecture · Frontend em **Angular 19 standalone + Material 19** · Persistência **SQLite** com tokens criptografados via **Data Protection** · **PWA** instalável.

## Screenshots

| Dashboard | Triagem | Modo Tinder | Share card |
| :--: | :--: | :--: | :--: |
| _(adicionar `docs/dashboard.png`)_ | _(adicionar `docs/triage.png`)_ | _(adicionar `docs/tinder.png`)_ | _(adicionar `docs/share.png`)_ |

## Features

- **Login Spotify OAuth 2.0** (Authorization Code + PKCE), tokens criptografados no servidor, cookie HttpOnly
- **Dashboard** com top artistas/faixas, donut de gêneros, popularidade, insights e tocadas recentes
- **Curtidas** com paginação infinita, filtro por ano, bulk delete
- **Playlists** listadas em grid, com detalhe e remoção em lote
- **Triagem multi-fonte**: scaneia curtidas + todas as playlists, mostra cada faixa com badges das origens, remove de tudo de uma vez
- **Modo Tinder** com swipe (pointer events), preview de áudio, undo, remoção em lote no final
- **Compartilhar** card-image 1080×1350 pra stories (html2canvas)
- **Dark/Light mode** com Material M3 theming
- **Export JSON** do histórico de remoções (localStorage)
- **Coletor de histórico** em background (`IHostedService`) acumulando recently-played pra filtros futuros tipo "não escuto há X"
- **PWA**: instalável no celular/desktop, manifest, service worker

## Stack

| Camada       | Tecnologia                                                |
| ------------ | --------------------------------------------------------- |
| Backend      | ASP.NET Core 10, EF Core 10, SQLite                       |
| Resiliência  | `Microsoft.Extensions.Http.Resilience` (Polly v8)         |
| Auth         | OAuth 2.0 (PKCE) + Cookie HttpOnly assinado               |
| Tokens       | Criptografados com `IDataProtector`                       |
| Frontend     | Angular 19 standalone, Angular Material 19, ng2-charts    |
| Estado       | Signals (sem RxJS Subject pra estado)                     |
| PWA          | Angular Service Worker (ngsw)                             |

## Estrutura

```
SpotifyDel/
├── README.md
├── Dockerfile           # backend prod build (multi-stage)
├── render.yaml          # deploy do backend no Render
├── backend/
│   ├── SpotifyDel.slnx
│   ├── src/
│   │   ├── SpotifyDel.Api/            # Controllers + Program.cs
│   │   ├── SpotifyDel.Application/    # Casos de uso, abstrações, DI
│   │   ├── SpotifyDel.Domain/         # Entidades (POCO)
│   │   └── SpotifyDel.Infrastructure/ # EF Core + Spotify HttpClient + coletor
│   └── tests/SpotifyDel.Tests/
└── frontend/spotify-del-app/
    ├── vercel.json      # deploy do frontend no Vercel
    ├── proxy.conf.json  # dev proxy /api → backend
    └── src/app/
        ├── core/                  # auth, theme, removal, api clients, models
        ├── shared/                # nav, avatar, skeleton, empty-state
        └── features/
            ├── login/
            ├── dashboard/         # cards, charts, share dialog
            ├── tracks/            # curtidas + filtros
            ├── playlists/         # list + detail
            ├── triage/            # scan multi-fonte + bulk delete
            └── tinder/            # swipe + audio preview
```

---

## Rodando local

### 1. Criar app no Spotify

1. https://developer.spotify.com/dashboard → **Create app**
2. **Redirect URI**: `http://127.0.0.1:5050/api/auth/callback` (literal `127.0.0.1`, não `localhost` — Spotify rejeita)
3. **APIs used**: marcar **Web API**
4. **Settings → User Management**: adicione seu email do Spotify (dev mode aceita até 5 usuários)
5. Copie **Client ID** e o **Client secret**

### 2. Configurar secrets do backend

```powershell
cd backend\src\SpotifyDel.Api
dotnet user-secrets set "Spotify:ClientId"     "<seu_client_id>"
dotnet user-secrets set "Spotify:ClientSecret" "<seu_client_secret>"
```

### 3. Rodar

```powershell
# Terminal 1 — backend
dotnet run --project backend/src/SpotifyDel.Api

# Terminal 2 — frontend
cd frontend/spotify-del-app
npm install
npm start
```

Acessar **http://127.0.0.1:4200** (não use `localhost` — quebra o cookie de sessão).

---

## Deploy

### Backend no Render

1. Push do repo pro GitHub
2. https://render.com → **New +** → **Blueprint** → conecte o repo
3. Render detecta `render.yaml` automaticamente
4. Em **Environment**, defina os secrets que estão marcados `sync: false`:
   - `Spotify__ClientId`
   - `Spotify__ClientSecret`
   - `Spotify__RedirectUri` = `https://<seu-app>.onrender.com/api/auth/callback`
   - `Frontend__BaseUrl` = `https://<seu-front>.vercel.app`
5. Deploy. Anote a URL (`https://spotifydel-api.onrender.com`).

> ⚠️ **Render Free tier**: dorme após 15min inativo (~30s pra acordar no próximo acesso) e o disco **não persiste** — toda vez que reiniciar, o SQLite zera e usuários precisam relogar. Pra produção real, troque por Fly.io (free com disco persistente) ou pague o plano Standard do Render.

### Frontend no Vercel

1. https://vercel.com → **New project** → conecte o repo
2. **Root Directory**: `frontend/spotify-del-app`
3. Vercel lê o `vercel.json` automaticamente
4. Antes do deploy, edite `src/environments/environment.production.ts` com a URL do backend Render:
   ```ts
   apiBase: 'https://spotifydel-api.onrender.com',
   ```
5. Commit + push → Vercel buildaa e publica

### Atualizar Spotify dashboard

No app do Spotify dashboard, adicione o novo Redirect URI de produção:
`https://<seu-backend>.onrender.com/api/auth/callback`

E o frontend público em **User Management** → adicione o email Spotify de cada usuário que vai testar (limite 5 em dev mode).

---

## Limitações conhecidas

- **Dev mode do Spotify**: só 5 usuários cadastrados manualmente podem usar o app. Sem isso retorna 403 silencioso em qualquer write.
- **Migração de endpoints Feb/2026**: `/me/tracks` (write/contains) virou `/me/library`; `/playlists/{id}/tracks` virou `/items`. Já tratado no código.
- **`preview_url` de track**: o Spotify não disponibiliza preview de áudio pra todas as faixas; quando não tem, o modo Tinder mostra "preview indisponível".
- **Recently played**: API só retorna últimas 50 reproduções. O coletor em background acumula localmente — em algumas semanas você terá dado suficiente pra filtros tipo "não escuto há X".

## Comandos úteis

```powershell
# Backend
dotnet build backend/SpotifyDel.slnx
dotnet test  backend/SpotifyDel.slnx

# Migration nova
dotnet ef migrations add <Nome> `
  --project backend/src/SpotifyDel.Infrastructure `
  --startup-project backend/src/SpotifyDel.Api `
  --output-dir Persistence/Migrations

# Apagar o banco local
Remove-Item backend/src/SpotifyDel.Api/spotifydel.db -ErrorAction SilentlyContinue

# Frontend
cd frontend/spotify-del-app
npm install --legacy-peer-deps
npm start              # dev em http://127.0.0.1:4200
npm run build          # gera dist/ pra produção

# Docker (testar imagem do backend localmente)
docker build -t spotifydel-api .
docker run -p 8080:8080 `
  -e Spotify__ClientId="..." -e Spotify__ClientSecret="..." `
  -e Spotify__RedirectUri="http://127.0.0.1:8080/api/auth/callback" `
  -e Frontend__BaseUrl="http://127.0.0.1:4200" `
  spotifydel-api
```

## Roadmap

- [x] **v0.1** — MVP (login, curtidas, filtros básicos, bulk delete)
- [x] **v0.2** — Playlists, triagem multi-fonte, modo Tinder
- [x] **v0.3** — Dashboard com charts, dark mode, share card-image
- [x] **v0.4** — PWA, coletor de histórico em background
- [ ] **v0.5** — Filtros baseados em histórico (depende do coletor acumular dados)
- [ ] **v1.0** — Undo de remoção via UI, deploy público com Extended Quota
