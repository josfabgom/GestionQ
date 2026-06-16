# Guía de Configuración: Servidor en Red Local

Sigue estos pasos en la PC que actuará como **Servidor Principal** para permitir que las demás PCs puedan acceder a la base de datos y a la aplicación web.

## 1. Configurar SQL Server Express para Conexiones de Red

Por defecto, SQL Server Express solo acepta conexiones locales. Para habilitar las conexiones remotas:

1. Abre **SQL Server Configuration Manager** (Administrador de configuración de SQL Server).
2. Expande el nodo **Configuración de red de SQL Server** y haz clic en **Protocolos de SQLEXPRESS**.
3. En el panel derecho, haz clic derecho en **TCP/IP** y selecciona **Habilitar**.
4. Vuelve a hacer clic derecho en **TCP/IP** y selecciona **Propiedades**.
5. Ve a la pestaña **Direcciones IP**:
   - Desplázate hacia abajo hasta la sección **IPAll**.
   - Borra cualquier valor que haya en "Puertos dinámicos TCP".
   - En **Puerto TCP**, escribe `1433`.
   - Haz clic en **Aceptar**.
6. En el panel izquierdo, ve a **Servicios de SQL Server**.
7. Haz clic derecho en **SQL Server (SQLEXPRESS)** y selecciona **Reiniciar**.

## 2. Configurar el Firewall de Windows (Abrir Puertos)

Necesitas permitir el tráfico entrante para SQL Server y para la aplicación Web. Puedes hacerlo abriendo una consola de PowerShell **como Administrador** y ejecutando los siguientes comandos:

```powershell
# Abrir puerto para SQL Server (1433)
New-NetFirewallRule -DisplayName "SQL Server (TCP 1433)" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow

# Abrir puerto para la Aplicación Web (Kestrel) (5000)
New-NetFirewallRule -DisplayName "GestionQ Web App (TCP 5000)" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

## 3. Ejecutar la Aplicación en el Servidor

1. Ejecuta el script `crear_publicacion_kestrel.ps1` que se encuentra en la raíz del proyecto para generar los ejecutables.
2. Ve a la carpeta `out\InstaladorKestrel` (o descomprime el archivo `.zip`).
3. Ejecuta el archivo **`GestionQ.Web.exe`**. Verás una ventana negra de consola (Kestrel) indicando que la aplicación está escuchando en el puerto 5000. **Esa ventana debe permanecer abierta** para que el sistema funcione.

## 4. Acceder desde otras PCs en la red

Para que las otras computadoras puedan usar el sistema:

1. Averigua la dirección IP de la PC Servidor. (Abre CMD y escribe `ipconfig`, busca la línea "Dirección IPv4", por ejemplo `192.168.1.100`).
2. En las otras PCs, abre el navegador web y escribe la IP seguida del puerto 5000. Por ejemplo: 
   `http://192.168.1.100:5000`
3. ¡El sistema de Gestión debería cargar correctamente!
