$ErrorActionPreference = "Stop"
$Port = 5144

$connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
if ($connection) {
    $procIds = $connection.OwningProcess | Select-Object -Unique
    foreach ($procId in $procIds) {
        $process = Get-Process -Id $procId -ErrorAction SilentlyContinue
        if ($process) {
            # Intentar obtener el proceso padre (generalmente dotnet.exe si se inició con 'dotnet run')
            try {
                $parentPid = (Get-CimInstance -ClassName Win32_Process -Filter "ProcessId = $procId").ParentProcessId
                if ($parentPid) {
                    $parentProcess = Get-Process -Id $parentPid -ErrorAction SilentlyContinue
                    if ($parentProcess -and $parentProcess.ProcessName -eq "dotnet") {
                        Stop-Process -Id $parentPid -Force
                    }
                }
            } catch {}
            
            # Detener el proceso principal que escucha en el puerto
            Stop-Process -Id $procId -Force
            Write-Host "Servidor detenido correctamente."
        }
    }
} else {
    Write-Host "El servidor no estaba en ejecución."
}
