param (
    [string]$solutionPath = "."
)

# Expande o caminho relativo para um caminho completo
$solutionPath = (Resolve-Path $solutionPath).Path

# Verifica se o caminho da solução existe
if (-not (Test-Path $solutionPath)) {
    Write-Host "O caminho especificado não existe." -ForegroundColor Red
    exit
}

# Limpar o cache do NuGet
Write-Host "Limpando o cache do NuGet..." -ForegroundColor Cyan
dotnet nuget locals all --clear

# Procurar por todas as pastas 'bin' e 'obj' e removê-las
Write-Host "Limpando pastas 'bin' e 'obj'..." -ForegroundColor Cyan
Get-ChildItem -Path $solutionPath -Recurse -Directory -Filter bin | ForEach-Object {
    Write-Host "Removendo: $($_.FullName)" -ForegroundColor Green
    Remove-Item $_.FullName -Recurse -Force
}

Get-ChildItem -Path $solutionPath -Recurse -Directory -Filter obj | ForEach-Object {
    Write-Host "Removendo: $($_.FullName)" -ForegroundColor Green
    Remove-Item $_.FullName -Recurse -Force
}

# Caminho da solução e do arquivo nuget.config
$solutionFile = Join-Path $solutionPath "Oha.Agents.Example.sln"
$nugetConfigFile = Join-Path $solutionPath "nuget.config"

# Verificar se a solução e o nuget.config existem
if (-not (Test-Path $solutionFile)) {
    Write-Host "O arquivo de solução não foi encontrado: $solutionFile" -ForegroundColor Red
    exit
}

if (-not (Test-Path $nugetConfigFile)) {
    Write-Host "O arquivo nuget.config não foi encontrado: $nugetConfigFile" -ForegroundColor Red
    exit
}

# Restaurar pacotes da solução com o arquivo nuget.config especificado
Write-Host "Restaurando pacotes NuGet para a solução..." -ForegroundColor Cyan
dotnet restore --configfile "$nugetConfigFile" "$solutionFile"

Write-Host "Limpeza e restauração concluídas!" -ForegroundColor Cyan
