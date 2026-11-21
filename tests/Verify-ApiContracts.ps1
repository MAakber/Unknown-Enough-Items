param(
    [Parameter(Mandatory)][string]$CecilPath,
    [Parameter(Mandatory)][string]$CuCoreLibPath,
    [Parameter(Mandatory)][string]$TogetherPath,
    [Parameter(Mandatory)][string]$PluginPath
)
$ErrorActionPreference = 'Stop'
Add-Type -Path $CecilPath

function Assert-StaticMethod($assembly, [string]$typeName, [string]$methodName, [string]$returnType, [string[]]$parameters) {
    $type = $assembly.MainModule.GetType($typeName)
    if (!$type -or !$type.IsPublic) { throw "Missing public type: $typeName" }
    $matches = @($type.Methods | Where-Object {
        $_.Name -eq $methodName -and $_.IsPublic -and $_.IsStatic -and $_.ReturnType.FullName -eq $returnType -and
        (($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -eq ($parameters -join ',')
    })
    if ($matches.Count -ne 1) { throw "API mismatch: $typeName.$methodName($($parameters -join ', '))" }
    Write-Output "PASS: $($matches[0].FullName)"
}

$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($CuCoreLibPath)
$together = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($TogetherPath)
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginPath)
try {
    Assert-StaticMethod $core 'CUCoreLib.Registries.ItemRegistry' 'TryGetIcon' 'System.Boolean' @('System.String','UnityEngine.Sprite&')
    Assert-StaticMethod $core 'CUCoreLib.Registries.ItemRegistry' 'TryGetCustomInfo' 'System.Boolean' @('System.String','CUCoreLib.Data.CustomItemInfo&')
    Assert-StaticMethod $core 'CUCoreLib.Registries.ItemRegistry' 'TryGetItemInfo' 'System.Boolean' @('System.String','ItemInfo&')
    Assert-StaticMethod $core 'CUCoreLib.Helpers.CustomInstantiate' 'InstantiateReturn' 'UnityEngine.GameObject' @('System.String','UnityEngine.Vector3','UnityEngine.Quaternion','System.Nullable`1<System.Single>')
    foreach($getter in @('get_IsAvailable','get_IsRunning','get_IsClient','get_IsServer')) {
        Assert-StaticMethod $core 'CUCoreLib.Networking.MultiplayerApi' $getter 'System.Boolean' @()
    }
    Assert-StaticMethod $together 'Together.Net' 'get_IsRunning' 'System.Boolean' @()
    Assert-StaticMethod $together 'Together.Net' 'get_IsClient' 'System.Boolean' @()
    Assert-StaticMethod $together 'Together.Con' 'CanExecuteAdminCommands' 'System.Boolean' @()
    Assert-StaticMethod $together 'Together.Con' 'ExecuteCommandAdminNoLog' 'System.Void' @('System.String&')
    $rules = @($together.MainModule.GetType('Together.Multiplayer').Fields | Where-Object { $_.Name -eq 'rules' -and $_.IsStatic })
    if ($rules.Count -ne 1 -or !($together.MainModule.GetType($rules[0].FieldType.FullName).Fields | Where-Object { $_.Name -eq 'sv_cheats' -and $_.FieldType.FullName -eq 'System.Boolean' })) {
        throw 'Together rules.sv_cheats contract mismatch'
    }
    Write-Output 'PASS: Together.Multiplayer.rules.sv_cheats'

    $version = ($plugin.MainModule.GetType('UeiPlugin').Fields | Where-Object Name -eq 'PluginVersion').Constant
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid plugin version: $version" }
    $metadata = $plugin.MainModule.GetType('UeiPlugin').CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' }
    if ($metadata.ConstructorArguments[2].Value -ne $version) { throw 'BepInPlugin version differs from constant' }
    if (!$plugin.MainModule.GetType('UeiGlobalDarkUpdatePatch')) { throw 'Compiled recovery hook missing' }
    if ($plugin.MainModule.AssemblyReferences | Where-Object { $_.Name -match '^(CUCoreLib|CasualtiesMP|RshLib|ScavPrototypeSexMod)$' }) { throw 'Optional mod became a mandatory assembly reference' }
    Write-Output "PASS: UEI $version metadata, compiled recovery hook, optional assembly references"
}
finally {
    $core.Dispose()
    $together.Dispose()
    $plugin.Dispose()
}
