$connectionString = "Server=localhost;Database=FodunReservas;Integrated Security=true;Encrypt=False;TrustServerCertificate=True;"
$scripts = @(
    "c:\Users\sebas\OneDrive\Escritorio\FodunReservar\Bd\SP habitacionesDisponiblesPorFechaYPersonas.sql",
    "c:\Users\sebas\OneDrive\Escritorio\FodunReservar\Bd\SP habitacionesDisponiblesPorFecha.sql",
    "c:\Users\sebas\OneDrive\Escritorio\FodunReservar\Bd\Sp ConsultarTarifas.sql",
    "c:\Users\sebas\OneDrive\Escritorio\FodunReservar\Bd\Sp CalcularTotalReservas.sql",
    "c:\Users\sebas\OneDrive\Escritorio\FodunReservar\Bd\SPfodun.sql"
)

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    foreach ($scriptPath in $scripts) {
        if (Test-Path $scriptPath) {
            Write-Host "Ejecutando: $(Split-Path $scriptPath -Leaf)..."
            $sqlContent = Get-Content -Path $scriptPath -Raw
            
            $batches = $sqlContent -split "GO\s*`n" | Where-Object { $_.Trim() }
            
            foreach ($batch in $batches) {
                if ($batch.Trim()) {
                    $command = $connection.CreateCommand()
                    $command.CommandText = $batch
                    $command.CommandTimeout = 60
                    $command.ExecuteNonQuery() | Out-Null
                }
            }
            Write-Host "OK: $(Split-Path $scriptPath -Leaf) ejecutado"
        } else {
            Write-Host "NO ENCONTRADO: $scriptPath"
        }
    }
    
    $connection.Close()
    Write-Host "`nTodos los procedimientos almacenados creados"
}
catch {
    Write-Host ("Error: " + $_.ToString())
}
