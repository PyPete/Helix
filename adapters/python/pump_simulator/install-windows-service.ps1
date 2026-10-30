param(
    [string]$NssmPath = "nssm",
    [string]$PythonExe = "C:\Python310\python.exe",
    [string]$AdapterScript = "C:\Helix\Adapters\Python\pump_simulator\pump_adapter.py"
)

$serviceName = "HelixPumpAdapter"
$adapterDirectory = Split-Path -Path $AdapterScript -Parent

& $NssmPath install $serviceName $PythonExe $AdapterScript
& $NssmPath set $serviceName AppDirectory $adapterDirectory
& $NssmPath set $serviceName DisplayName "Helix Pump Simulator Adapter"
& $NssmPath set $serviceName Start SERVICE_AUTO_START
& $NssmPath start $serviceName
