# Rollback y recuperación

Qué hacer cuando un deploy a producción sale mal. Todo lo de acá ya pasó de verdad en este
proyecto (ver los incidentes reales referenciados en cada sección) — no es teoría.

## 1. El deploy corrió pero rompió algo (código malo en `main`)

`deploy.yml` solo dispara con push a `main`. Para volver atrás:

```bash
git checkout main
git pull origin main
git log --oneline -5          # identificar el merge commit problemático
git revert -m 1 <hash-del-merge-commit>
git push origin main
```

El `revert` dispara `deploy.yml` de nuevo, esta vez con el código anterior. No hace falta
tocar el server a mano — el mismo pipeline pull-based que rompió las cosas las arregla.

**Si el revert no alcanza** (por ejemplo, una migración de base ya se aplicó y el código
viejo no es compatible con el schema nuevo): ver la sección 3.

## 2. El deploy falló y no llegó a levantar el contenedor

Con `git fetch` + `reset --hard` (en vez de `git pull`, ver el porqué en el
[README, sección 4](../README.md#4-pipeline-cicd--github--deploy)) el checkout del server
siempre debería estar limpio. Si igual falla:

1. Revisar el log del run en GitHub Actions → pestaña *Actions* → el run de
   `Deploy MiniStock API` que falló. El step "Deploy to Hetzner via SSH" tiene el output
   completo de `docker compose up --build`.
2. Causas típicas:
   - **Migración de EF Core falla al aplicarse** (`db.Database.MigrateAsync()` en
     `Program.cs`, corre en el startup) — el contenedor de la API no levanta. Ver sección 3.
   - **Build de Docker falla** — casi siempre un error de compilación que `ci.yml` debería
     haber atajado antes en el PR; revisar que el PR que se mergeó a `main` haya pasado CI
     en verde.
   - **`docker compose` no puede levantar por falta de un secret** (ej. `CORS_ORIGINS` no
     seteado en GitHub — el step aborta antes de tocar el contenedor, con un mensaje
     explícito en el log).
3. Mientras se diagnostica, el contenedor **anterior sigue corriendo** — `docker compose up`
   no lo tira abajo hasta que el nuevo build termine con éxito, así que un deploy roto no
   deja el sitio caído por sí solo (salvo que la migración fallida sea la causa — ver abajo).

## 3. Una migración de EF Core falla al arrancar

Esto **ya pasó** con `AddCategoryNameUniqueIndexAndUserRefreshTokenIndex` (2026-09-08): si
la migración no puede aplicarse (por ejemplo, un índice único que choca con datos
existentes), `db.Database.MigrateAsync()` tira una excepción, el `try/catch` de
`Program.cs` la loguea como `Fatal` y el proceso termina — el contenedor de la API queda en
loop de reinicio (`restart: unless-stopped` en `docker-compose.yml`).

**Diagnóstico:** `docker compose -f deploy/docker-compose.yml logs api` en el server, o el
log del step de deploy en Actions si pasó ahí.

**Recuperación, dado que hoy la base de producción es solo datos de demo** (sin usuarios ni
inventario real todavía — si esto cambia, esta sección hay que reescribirla):

```bash
# En el server:
cd /opt/ministock
docker compose -f deploy/docker-compose.yml down -v   # -v: también borra el volumen pgdata
docker compose -f deploy/docker-compose.yml up --build -d
```

**Importante:** `DatabaseSeeder.SeedAsync` (el que crea `admin@ministock.com` + categorías/
productos demo) solo corre `if (app.Environment.IsDevelopment())` — en producción
(`ASPNETCORE_ENVIRONMENT=Production`) **no se ejecuta**. Un reset en producción deja la base
completamente vacía, sin usuario Admin. Hay que recrearlo a mano (por ejemplo, un insert SQL
directo con un hash de BCrypt, o correr el seeder puntualmente contra la DB de producción
como paso manual) antes de anunciar el demo como disponible de nuevo.

## 4. Backup/restore de la base de datos

**No implementado todavía.** Hoy `pgdata` es un volumen de Docker sin backup automático —
si el volumen se corrompe o se borra sin querer (fuera del `down -v` intencional de arriba),
se pierde todo. Dado que la base es de datos de demo por ahora, el impacto real es bajo,
pero si este proyecto pasa a tener datos que importa preservar, este es el primer punto a
resolver antes de esa transición — no antes.
