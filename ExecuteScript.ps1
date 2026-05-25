$connectionString = "Server=localhost;Database=FodunReservas;Integrated Security=true;Encrypt=False;TrustServerCertificate=True;"
$scriptPath = "c:\Users\sebas\OneDrive\Escritorio\FodunReservar\Bd\BdScript.sql"

try {
    $sqlContent = Get-Content -Path $scriptPath -Raw
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $batches = $sqlContent -split "GO\s*`n" | Where-Object { $_.Trim() }
    
    foreach ($batch in $batches) {
        if ($batch.Trim()) {
            $command = $connection.CreateCommand()
            $command.CommandText = $batch
            $command.CommandTimeout = 60
            $command.ExecuteNonQuery() | Out-Null
            Write-Host "Batch ejecutado exitosamente"
        }
    }
    
    $connection.Close()
    Write-Host "Script ejecutado exitosamente"
}
catch {
    Write-Host "Error: $_"
}
