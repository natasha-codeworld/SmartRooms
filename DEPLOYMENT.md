# SmartRooms — Deployment Guide

## Option 1: Railway (Recommended — Free, Easiest)

Railway is the easiest free deployment for Docker Compose projects.

### Step 1 — Push to GitHub

```bash
cd SmartRooms
git init
git add .
git commit -m "feat: initial SmartRooms booking system"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/SmartRooms.git
git push -u origin main
```

### Step 2 — Deploy on Railway

1. Go to https://railway.app and sign up with GitHub
2. Click **New Project** → **Deploy from GitHub Repo**
3. Select your `SmartRooms` repository
4. Railway detects `docker-compose.yml` automatically
5. Click **Deploy** — wait ~3 minutes
6. Go to **Settings** → **Domains** → **Generate Domain**
7. Your live URL will be: `https://smartrooms-xxxx.railway.app`

### Step 3 — Set Environment Variables on Railway

In Railway dashboard → your project → **Variables**, add:

```
SA_PASSWORD=SmartRooms@123
JwtKey=SuperSecretKeyForSmartRooms2024XYZ!
JwtIssuer=SmartRooms
JwtAudience=SmartRoomsUsers
```

### Step 4 — Test your live API

```
GET https://smartrooms-xxxx.railway.app/api/rooms
GET https://smartrooms-xxxx.railway.app/health
```

---

## Option 2: Render (Also Free)

1. Go to https://render.com → New → Web Service
2. Connect your GitHub repo
3. Select **Docker** as environment
4. Set root directory as `.` (root)
5. Add environment variables same as above
6. Click **Create Web Service**

Free tier: service sleeps after 15 min inactivity (wakes on request).

---

## Option 3: Azure (Free $200 credits for 30 days)

### Prerequisites
```bash
# Install Azure CLI
winget install Microsoft.AzureCLI

# Login
az login
```

### Deploy
```bash
# Create resource group
az group create --name SmartRoomsRG --location eastus

# Create container registry
az acr create --resource-group SmartRoomsRG --name smartroomsacr --sku Basic

# Login to registry
az acr login --name smartroomsacr

# Build and push images
docker compose build
docker tag smartrooms-roomservice smartroomsacr.azurecr.io/roomservice:latest
docker tag smartrooms-bookingservice smartroomsacr.azurecr.io/bookingservice:latest
docker tag smartrooms-userservice smartroomsacr.azurecr.io/userservice:latest
docker tag smartrooms-apigateway smartroomsacr.azurecr.io/apigateway:latest

docker push smartroomsacr.azurecr.io/roomservice:latest
docker push smartroomsacr.azurecr.io/bookingservice:latest
docker push smartroomsacr.azurecr.io/userservice:latest
docker push smartroomsacr.azurecr.io/apigateway:latest

# Deploy to Azure Container Apps
az containerapp env create \
  --name SmartRoomsEnv \
  --resource-group SmartRoomsRG \
  --location eastus
```

---

## Run Locally (Docker)

```bash
# Clone
git clone https://github.com/YOUR_USERNAME/SmartRooms.git
cd SmartRooms

# Start everything
docker compose up --build

# Wait ~30 seconds for SQL Server to start, then test:
curl http://localhost:5000/api/rooms
curl http://localhost:5000/health
```

### Access Swagger UI (individual services)
When running locally, each service also exposes its own Swagger UI:

| Service | URL |
|---|---|
| API Gateway | http://localhost:5000 |
| Room Service | http://localhost:5001/swagger |
| Booking Service | http://localhost:5002/swagger |
| User Service | http://localhost:5003/swagger |

To expose individual service ports locally, add these to `docker-compose.yml`:
```yaml
roomservice:
  ports:
    - "5001:8080"
bookingservice:
  ports:
    - "5002:8080"
userservice:
  ports:
    - "5003:8080"
```

---

## What to put on your resume

> **Live demo:** https://smartrooms-xxxx.railway.app/api/rooms
