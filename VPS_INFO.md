# Arquitectura GestionQ - Web & Local

Este archivo sirve como "memoria" para que el asistente de IA o cualquier desarrollador sepa cómo está estructurado el proyecto y cómo conectarse rápidamente.

## 🌍 VPS (Hostinger)
- **IP:** 179.199.138.79
- **Dominio:** `galopsrl.com.ar`
- **Usuario SSH:** `root`
- **Contraseña SSH:** `Herrera4480+`
- **Sistema Operativo:** Ubuntu

### Stack Tecnológico en la VPS
- **Nginx:** Actúa como Proxy Reverso. Escucha en el puerto `80`.
- **Cloudflare:** Provee el certificado HTTPS (Modo "Flexible") y redirige el tráfico al puerto 80 del VPS.
- **Docker & Docker Compose:** Levantan la app web en el puerto `3000` (mapeado al 3000 del host).
- **Aplicación Web:** Next.js (App Router) + TailwindCSS.
- **Base de Datos Web:** SQLite (`prisma/dev.db`), la cual persiste gracias a los volúmenes de Docker.

## 💻 API Local (C# / .NET)
- El proyecto `GestionQ.Api` contiene un `StatsSyncService.cs` (Background Service).
- Cada 1 minuto recopila datos de ventas (`Summary`, `PosBoxes`, `PosHistory`).
- Hace una petición `POST` a `https://galopsrl.com.ar/api/sync` utilizando un `X-Api-Key` para inyectar los datos en el dashboard web.

## 🚀 Cómo hacer despliegues (Deploy)
Simplemente ejecuta el script que se encuentra en la raíz del proyecto:
```powershell
.\Deploy-Dashboard.ps1
```
Este script automáticamente:
1. Comprime los archivos de Next.js (excluyendo `node_modules` y `.next`).
2. Usa `deploy.js` para conectarse por SSH al VPS.
3. Sube el `.tar.gz`, lo descomprime y ejecuta `docker-compose up -d --build`.
