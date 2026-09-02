# Configuración inicial del servidor Hetzner

Ejecutar una sola vez al preparar el servidor. **Ya ejecutado el 2026-09-02** en `portfolio-hel1-1` (`2.29.23.254`) — este documento queda como referencia/histórico.

## 1. Instalar dependencias (Ubuntu 26.04)

```bash
apt update && apt upgrade -y
apt install -y git curl ca-certificates ufw fail2ban

# Docker
curl -fsSL https://get.docker.com | sh
usermod -aG docker deploy
```

Server hardening (usuario no-root `deploy`, `ufw`, `fail2ban`, swap) — ver `docs/INFRAESTRUCTURA.md` en la raíz de `D:\Code\projects` para el detalle completo del proceso.

## 2. Clonar el repositorio

```bash
sudo mkdir -p /opt/ministock && sudo chown deploy:deploy /opt/ministock
git clone https://github.com/tech-marcos-rios/ministock-api.git /opt/ministock
```

## 3. Crear el archivo de secretos

```bash
cat > /opt/ministock/deploy/.env << 'EOF'
DB_PASSWORD=CAMBIAR_POR_PASSWORD_SEGURO
JWT_KEY=CAMBIAR_POR_CLAVE_MINIMO_32_CARACTERES_ALEATORIA
CORS_ORIGINS=https://TU_FRONTEND.vercel.app
EOF
chmod 600 /opt/ministock/deploy/.env
```

**Nota:** `CORS_ORIGINS` quedó en `http://localhost:3000` como placeholder hasta que el frontend de MiniStock tenga URL de Vercel definitiva — actualizar ese valor en el server (`/opt/ministock/deploy/.env`) antes de ir a producción real.

## 4. Primer deploy

```bash
cd /opt/ministock
docker compose -f deploy/docker-compose.yml up --build -d
```

## 5. Verificar

```bash
docker compose -f deploy/docker-compose.yml ps
curl http://localhost:5010/health
```

## Secrets requeridos en GitHub Actions

Ir a: Settings → Secrets → Actions → New repository secret

| Secret | Valor |
|--------|-------|
| `HETZNER_HOST` | `2.29.23.254` |
| `HETZNER_USER` | `deploy` |
| `HETZNER_SSH_KEY` | Clave privada SSH dedicada (`p_portfolio_hetzner`, sin passphrase) |

La clave pública correspondiente (`p-portfolio-root`) ya está en `/home/deploy/.ssh/authorized_keys` en el servidor. **Root ya no acepta login por SSH** en este server — solo el usuario `deploy`.
