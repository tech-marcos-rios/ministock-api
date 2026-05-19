# Configuración inicial del servidor Hetzner

Ejecutar una sola vez al preparar el servidor.

## 1. Instalar dependencias (Ubuntu 22.04)

```bash
apt update && apt upgrade -y
apt install -y git curl ca-certificates

# Docker
curl -fsSL https://get.docker.com | sh
usermod -aG docker $USER
```

## 2. Clonar el repositorio

```bash
mkdir -p /opt/ministock
cd /opt/ministock
git clone https://github.com/tech-marcos-rios/ministock-api.git .
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
| `HETZNER_HOST` | `204.168.134.159` |
| `HETZNER_USER` | `root` |
| `HETZNER_SSH_KEY` | Clave privada SSH (sin passphrase) |

La clave pública correspondiente debe estar en `/root/.ssh/authorized_keys` en el servidor.
