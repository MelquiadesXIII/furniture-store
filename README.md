# Furnistore

Monorepo gestionado con [Turborepo](https://turbo.build) y pnpm.

## Estructura

```
apps/
  api/   ASP.NET Core Web API — .NET 10, EF Core, PostgreSQL   (@furnistore/api)
  web/   Next.js — App Router, TypeScript                       (@furnistore/web)
packages/
  (compartidos: api-client, config, ui — por añadir)
```

La estructura interna de `apps/api` (solución `.sln` + los 3 proyectos
`API.Furnistore.*`) se mantiene tal cual; solo se movió dentro de `apps/api/`.

## Requisitos

| Herramienta | Versión | Necesaria para |
|-------------|---------|----------------|
| .NET SDK    | 10.0.x (ver `apps/api/global.json`) | `apps/api` |
| Node.js     | 20+ (ver `.node-version`)           | `apps/web`, Turborepo |
| pnpm        | 9 (`corepack enable`)              | workspace |

Para trabajar **solo en la API** basta el .NET SDK; para **solo la web**, Node + pnpm.

## Comandos

Desde la raíz (orquestado por Turborepo):

```bash
pnpm install                              # dependencias JS de todo el workspace
pnpm dev                                  # API + web a la vez
pnpm dev --filter=@furnistore/web    # solo web
pnpm dev --filter=@furnistore/api    # solo API (envuelve dotnet watch)
pnpm build                               # compila todo
```

Solo API, sin tocar Node/Turborepo:

```bash
cd apps/api
dotnet restore API.sln
dotnet build API.sln
dotnet watch --project API.Furnistore.API/API.Furnistore.API.csproj run
```

## Variables de entorno

Copiar `.env.example` a `.env` y definir `DATABASE_URL`, `JWT_SECRET`,
`JWT_ISSUER`, `JWT_AUDIENCE`. La API las lee del entorno, con `appsettings` como
fallback.
