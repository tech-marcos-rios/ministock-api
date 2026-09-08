# Configuración inicial del servidor Hetzner

Ejecutar una sola vez al preparar el servidor. **Ya ejecutado** — este documento queda
como referencia/histórico. Los valores reales del servidor (IP, hostname, usuarios) viven
únicamente en GitHub Secrets y en la documentación privada de infraestructura, no en este
repo público.

## 1. Instalar dependencias (Ubuntu 26.04)

```bash
apt update && apt upgrade -y
apt install -y git curl ca-certificates ufw fail2ban

# Docker
curl -fsSL https://get.docker.com | sh
usermod -aG docker deploy
```

Server hardening (usuario de deploy sin privilegios root, `ufw`, `fail2ban`, swap) — ver la
documentación privada de infraestructura para el detalle completo del proceso.

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

**Nota:** `CORS_ORIGINS` debe apuntar a la URL real de Vercel del frontend antes de ir a
producción — no dejarlo en un valor de desarrollo.

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

| Secret | Qué va acá |
|--------|------------|
| `HETZNER_HOST` | IP o hostname del servidor (valor real solo en el secret, no documentado acá) |
| `HETZNER_USER` | Usuario de deploy sin privilegios root |
| `HETZNER_SSH_KEY` | Clave privada SSH dedicada para este deploy, sin passphrase |

La clave pública correspondiente ya está en `~/.ssh/authorized_keys` del usuario de deploy
en el servidor. El login por SSH como `root` está deshabilitado — solo se acepta el usuario
de deploy, y solo por clave (sin autenticación por password).
