param(
    [Parameter(Mandatory)][string]$CecilPath,
    [Parameter(Mandatory)][string]$CuCoreLibPath,
    [Parameter(Mandatory)][string]$TogetherPath,
    [Parameter(Mandatory)][string]$PluginPath,
    [string]$GameAssemblyPath
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

function Assert-InstanceMethod($assembly, [string]$typeName, [string]$methodName, [string]$returnType, [string[]]$parameters) {
    $methods = @($assembly.MainModule.GetType($typeName).Methods | Where-Object {
        $_.Name -eq $methodName -and $_.IsPublic -and !$_.IsStatic -and $_.ReturnType.FullName -eq $returnType -and
        (($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -eq ($parameters -join ',')
    })
    if ($methods.Count -ne 1) { throw "Instance API mismatch: $typeName.$methodName" }
    Write-Output "PASS: $($methods[0].FullName)"
}

function Assert-Field($assembly, [string]$typeName, [string]$fieldName, [string]$fieldType) {
    $type = $assembly.MainModule.GetType($typeName)
    $fields = @($type.Fields | Where-Object { $_.Name -eq $fieldName -and $_.IsPublic -and $_.FieldType.FullName -eq $fieldType })
    if ($fields.Count -ne 1) { throw "Game field mismatch: $typeName.$fieldName ($fieldType)" }
    Write-Output "PASS: $($fields[0].FullName)"
}

function Assert-Property($assembly, [string]$typeName, [string]$propertyName, [string]$propertyType) {
    $properties = @($assembly.MainModule.GetType($typeName).Properties | Where-Object {
        $_.Name -eq $propertyName -and $_.PropertyType.FullName -eq $propertyType -and $_.GetMethod.IsPublic
    })
    if ($properties.Count -ne 1) { throw "Unity property mismatch: $typeName.$propertyName" }
    Write-Output "PASS: $($properties[0].FullName)"
}

$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($CuCoreLibPath)
$together = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($TogetherPath)
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginPath)
$game = $null
$unity = $null
$tmp = $null
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
    Assert-StaticMethod $together 'Together.Con' 'GetCommandCallerPlayer' 'Together.ScavPlayer' @()
    Assert-StaticMethod $together 'Together.Con' 'LogToConsole' 'System.Void' @('System.String')
    Assert-StaticMethod $together 'CasualtiesTogetherUtils.Util_BodyExtensions' 'ForceAutoPickUpItem' 'System.Void' @('Body','Item')
    Assert-Field $together 'Together.ScavPlayer' 'body' 'Body'
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

    if ($GameAssemblyPath) {
        $game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($GameAssemblyPath)
        $camera = $game.MainModule.GetType('PlayerCamera')
        $raycasts = 'System.Collections.Generic.List`1<UnityEngine.EventSystems.RaycastResult>'
        $targets = @(
            @{ Patch = 'UeiPlayerCameraInputPatch'; Method = 'HandleInput'; Parameters = @(); PrefixReturn = 'System.Void'; PrefixParameters = @('PlayerCamera') },
            @{ Patch = 'UeiPlayerCameraAttackPatch'; Method = 'HandleAttacks'; Parameters = @(); PrefixReturn = 'System.Boolean'; PrefixParameters = @() },
            @{ Patch = 'UeiPlayerCameraStartDragPatch'; Method = 'HandleStartDragging'; Parameters = @($raycasts, 'UsableObject&'); PrefixReturn = 'System.Boolean'; PrefixParameters = @() },
            @{ Patch = 'UeiPlayerCameraReleaseDragPatch'; Method = 'HandleReleaseDragging'; Parameters = @($raycasts); PrefixReturn = 'System.Boolean'; PrefixParameters = @('PlayerCamera') },
            @{ Patch = 'UeiPlayerCameraWorldActionPatch'; Method = 'TryPerformWorldActions'; Parameters = @(); PrefixReturn = 'System.Boolean'; PrefixParameters = @() },
            @{ Patch = 'UeiPlayerCameraPlaceGhostPatch'; Method = 'HandleBlockPlaceGhost'; Parameters = @(); PrefixReturn = 'System.Boolean'; PrefixParameters = @('PlayerCamera') }
        )
        foreach ($target in $targets) {
            $methods = @($camera.Methods | Where-Object { $_.Name -eq $target.Method })
            if ($methods.Count -ne 1 -or $methods[0].IsStatic -or $methods[0].ReturnType.FullName -ne 'System.Void' -or
                (($methods[0].Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -ne ($target.Parameters -join ',')) {
                throw "Full-game input target signature mismatch: $($target.Method)"
            }
            $patch = $plugin.MainModule.GetType($target.Patch)
            if (!$patch) { throw "Compiled input patch missing: $($target.Patch)" }
            $attributes = @($patch.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' })
            if ($attributes.Count -ne 1 -or $attributes[0].ConstructorArguments[0].Value.FullName -ne 'PlayerCamera' -or
                $attributes[0].ConstructorArguments[1].Value -ne $target.Method) {
                throw "Compiled input patch targets the wrong game method: $($target.Patch)"
            }
            $prefix = @($patch.Methods | Where-Object { $_.Name -eq 'Prefix' -and $_.IsStatic })
            if ($prefix.Count -ne 1 -or $prefix[0].ReturnType.FullName -ne $target.PrefixReturn -or
                (($prefix[0].Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -ne ($target.PrefixParameters -join ',')) {
                throw "Compiled input prefix signature mismatch: $($target.Patch)"
            }
            if ($target.PrefixParameters.Count -gt 0 -and $prefix[0].Parameters[0].Name -ne '__instance') {
                throw "Compiled input prefix has no Harmony instance parameter: $($target.Patch)"
            }
            Write-Output "PASS: full-game $($methods[0].FullName) and compiled $($target.Patch)"
        }

        foreach ($field in @(
            @('BuildingEntity','itemsDropOnDestroy','ItemDrop[]'),
            @('BuildingEntity','alwaysDrop','ItemDrop[]'),
            @('BuildingEntity','dropChanceMultiplier','System.Single'),
            @('BuildingEntity','guaranteedDropAmount','System.Int32'),
            @('BuildingEntity','itemCategoriesToAdd','System.String[]'),
            @('BuildingEntity','id','System.String'),
            @('BuildingEntity','fullName','System.String'),
            @('BuildingEntity','description','System.String'),
            @('TraderScript','MoveRange','RangeF'),
            @('TraderScript','farEnoughToMove','System.Boolean'),
            @('TraderScript','character','System.Int32'),
            @('TraderScript','headRender','UnityEngine.SpriteRenderer'),
            @('RangeF','min','System.Single'),
            @('RangeF','max','System.Single'),
            @('Command','action','Command/Action'),
            @('BuildingEntity','animal','System.Boolean'),
            @('BuildingEntity','indestructible','System.Boolean'),
            @('ItemDrop','id','System.String'),
            @('ItemDrop','chance','System.Single'),
            @('ItemDrop','conditionMin','System.Single'),
            @('ItemDrop','conditionMax','System.Single'),
            @('ItemLootPool','pool','System.Collections.Generic.Dictionary`2<System.String,System.Collections.Generic.List`1<System.String>>'),
            @('ItemInfo','value','System.Int32'),
            @('ItemInfo','untradeable','System.Boolean'),
            @('LiquidItemInfo','capacity','System.Single'),
            @('LiquidItemInfo','defaultContents','System.Collections.Generic.List`1<LiquidStack>'),
            @('LiquidStack','liquidId','System.String'),
            @('LiquidStack','amount','System.Single')
        )) { Assert-Field $game $field[0] $field[1] $field[2] }
        Assert-StaticMethod $game 'Locale' 'GetBuilding' 'System.String' @('System.String')
        Assert-StaticMethod $game 'Item' 'GetItem' 'ItemInfo' @('System.String')
        Assert-StaticMethod $game 'Liquids' 'LiquidExists' 'System.Boolean' @('System.String')
        Assert-InstanceMethod $game 'WaterContainerItem' 'AddLiquid' 'System.Single' @('System.String','System.Single')
        Assert-InstanceMethod $game 'WaterContainerItem' 'DrainAll' 'System.Void' @()
        Assert-Property $game 'WaterContainerItem' 'Capacity' 'System.Single'
        Assert-Property $game 'Body' 'alive' 'System.Boolean'
        Assert-Property $game 'PauseHandler' 'paused' 'System.Boolean'
        Assert-Field $game 'MinigameBase' 'main' 'MinigameBase'
        Assert-Field $game 'MinigameBase' 'currentMinigame' 'Minigame'
        Assert-StaticMethod $game 'Minigame' 'op_Implicit' 'System.Boolean' @('Minigame')
        Assert-StaticMethod $game 'UIUtil' 'GetEventSystemRaycastResults' $raycasts @('System.Nullable`1<UnityEngine.Vector2>')
        Assert-Field $game 'PlayerCamera' 'radialOpen' 'System.Boolean'
        Assert-Field $game 'PlayerCamera' 'mainCanvas' 'UnityEngine.Canvas'
        Assert-Field $game 'GlobalDark' 'tooltipDescObj' 'TMPro.TextMeshProUGUI'
        foreach ($required in @('UeiEntities','UeiEntityPlacement','UeiWorldSpawning','TogetherSpawnHelper','UeiTraderSpawnInitializationPatch')) {
            if (!$plugin.MainModule.GetType($required)) { throw "Compiled entity/liquid implementation missing: $required" }
        }
        $traderStart = @($game.MainModule.GetType('TraderScript').Methods | Where-Object {
            $_.Name -eq 'Start' -and !$_.IsStatic -and $_.ReturnType.FullName -eq 'System.Void' -and $_.Parameters.Count -eq 0
        })
        $traderPatch = $plugin.MainModule.GetType('UeiTraderSpawnInitializationPatch')
        $traderAttribute = @($traderPatch.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' })
        $traderPrefix = @($traderPatch.Methods | Where-Object { $_.Name -eq 'Prefix' -and $_.IsStatic })
        if ($traderStart.Count -ne 1 -or $traderAttribute.Count -ne 1 -or
            $traderAttribute[0].ConstructorArguments[0].Value.FullName -ne 'TraderScript' -or
            $traderAttribute[0].ConstructorArguments[1].Value -ne 'Start' -or
            $traderPrefix.Count -ne 1 -or $traderPrefix[0].Parameters.Count -ne 1 -or
            $traderPrefix[0].Parameters[0].ParameterType.FullName -ne 'TraderScript' -or
            $traderPrefix[0].Parameters[0].Name -ne '__instance') { throw 'Trader spawn initialization patch contract mismatch' }
        $liquidRecipe = $game.MainModule.GetType('RecipeResult').Methods | Where-Object Name -eq 'SpawnResult'
        if (!($liquidRecipe.Body.Instructions | Where-Object { $_.OpCode.Code.ToString() -eq 'Ldstr' -and $_.Operand -eq 'craftingbottle' }) -or
            !($liquidRecipe.Body.Instructions | Where-Object { $_.OpCode.Code.ToString() -eq 'Ldc_R4' -and $_.Operand -eq 300 })) {
            throw 'Native temporary-bottle resource/lifetime behavior changed; review liquid spawning'
        }
        $panelType = $plugin.MainModule.GetType('UeiPanel')
        $dispatch = $panelType.Methods | Where-Object Name -eq 'TryCheatGiveItem'
        $dispatchCalls = @($dispatch.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -in @('Call','Callvirt')
        } | ForEach-Object { $_.Operand.Name })
        $click = $panelType.Methods | Where-Object Name -eq 'HandleEntryLeftClick'
        $clickCalls = @($click.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -in @('Call','Callvirt')
        } | ForEach-Object { $_.Operand.Name })
        if ([array]::IndexOf($clickCalls, 'BeginEntityPlacement') -lt 0 -or
            [array]::IndexOf($clickCalls, 'BeginEntityPlacement') -gt [array]::IndexOf($clickCalls, 'TryCheatGiveItem') -or
            [array]::IndexOf($dispatchCalls, 'TryCheatSpawnEntity') -ge 0 -or
            [array]::IndexOf($dispatchCalls, 'TryCheatGiveLiquid') -lt 0 -or
            [array]::IndexOf($dispatchCalls, 'TryCheatGiveLiquid') -gt [array]::IndexOf($dispatchCalls, 'TryGetLegalCheatItem')) {
            throw 'Compiled entity selection must enter placement while liquid clicks retain their give route'
        }
        $spawnAt = $panelType.Methods | Where-Object Name -eq 'TryCheatSpawnEntity'
        if ($spawnAt.Parameters.Count -ne 2 -or $spawnAt.Parameters[1].ParameterType.FullName -ne 'UnityEngine.Vector2') {
            throw 'Entity dispatch does not accept the selected world position'
        }
        foreach ($route in @('EntityCommand','SpawnEntity')) {
            $routeCalls = @($spawnAt.Body.Instructions | Where-Object {
                $_.OpCode.Code.ToString() -in @('Call','Callvirt') -and $_.Operand.Name -eq $route
            })
            if ($routeCalls.Count -ne 1 -or $routeCalls[0].Previous.OpCode.Code.ToString() -ne 'Ldarg_1') {
                throw "Entity route substitutes a different position: $route"
            }
        }
        $begin = $plugin.MainModule.GetType('UeiEntityPlacement').Methods | Where-Object Name -eq 'TryBegin'
        if ($begin.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -in @('Call','Callvirt') -and
            $_.Operand.Name -in @('SpawnEntity','TryCheatSpawnEntity','EntityCommand','Create','Instantiate')
        }) { throw 'Selecting an entity must not instantiate gameplay objects' }
        $spawn = $plugin.MainModule.GetType('UeiWorldSpawning').Methods | Where-Object Name -eq 'SpawnEntity'
        if (!($spawn.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -eq 'Ldstr' -and $_.Operand -eq 'spawn'
        }) -or !($spawn.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -eq 'Ldfld' -and $_.Operand.Name -eq 'action'
        })) { throw 'Compiled local entity path no longer executes the registered native spawn action' }
        Write-Output 'PASS: compiled placement selection, explicit local/server coordinates, native spawn action and trader initialization target'

        $tmp = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path ([System.IO.Path]::GetDirectoryName($GameAssemblyPath)) 'Unity.TextMeshPro.dll'))
        foreach ($property in @(
            @('richText','System.Boolean'), @('font','TMPro.TMP_FontAsset'),
            @('fontSharedMaterial','UnityEngine.Material'), @('spriteAsset','TMPro.TMP_SpriteAsset'),
            @('parseCtrlCharacters','System.Boolean')
        )) { Assert-Property $tmp 'TMPro.TMP_Text' $property[0] $property[1] }
        $textCard = $panelType.Methods | Where-Object Name -eq 'AddAcquisitionTextCard'
        if ($textCard.Parameters.Count -ne 6 -or $textCard.Parameters[5].ParameterType.FullName -ne 'System.Boolean' -or
            !$textCard.Parameters[5].IsOptional -or $textCard.Parameters[5].Constant -ne $false) {
            throw 'Rich-text rendering must remain opt-in for original entity descriptions'
        }
        $renderSetter = @($textCard.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -in @('Call','Callvirt') -and $_.Operand.Name -eq 'set_richText'
        })
        $measure = @($textCard.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -in @('Call','Callvirt') -and $_.Operand.Name -eq 'GetPreferredValues'
        })
        if ($renderSetter.Count -ne 1 -or $measure.Count -ne 1 -or $renderSetter[0].Offset -ge $measure[0].Offset -or
            $renderSetter[0].Previous.Operand.Name -ne 'renderRichText') {
            throw 'Original markup must be rendered before measuring scroll content'
        }
        $detail = $panelType.Methods | Where-Object Name -eq 'RefreshDetail'
        $richCardCalls = @($detail.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -in @('Call','Callvirt') -and $_.Operand.Name -eq 'AddAcquisitionTextCard' -and
                $_.Previous.OpCode.Code.ToString() -eq 'Ldc_I4_1'
        })
        if ($richCardCalls.Count -ne 1) { throw 'Exactly the entity description card must opt into rich-text rendering' }
        Write-Output 'PASS: original entity rich-text rendering, native font/sprite contracts and rendered-height measurement'
        if (!$plugin.MainModule.GetType('UeiNativeAcquisition') -or !$plugin.MainModule.GetType('UeiResourceIndex')) {
            throw 'Compiled native acquisition implementation missing'
        }

        # Compare the small copied selection-rule arrays with the actual shipped
        # methods. Any changed trader literals require a fresh behavior review.
        $ruleType = $plugin.MainModule.GetType('UeiAcquisitionIndex')
        $initializer = $ruleType.Methods | Where-Object Name -eq '.cctor'
        $rules = @{}
        $strings = [System.Collections.Generic.List[string]]::new()
        foreach ($instruction in $initializer.Body.Instructions) {
            if ($instruction.OpCode.Code.ToString() -eq 'Ldstr') { $strings.Add([string]$instruction.Operand) }
            if ($instruction.OpCode.Code.ToString() -eq 'Stsfld') {
                $rules[$instruction.Operand.Name] = $strings.ToArray()
                $strings.Clear()
            }
        }
        $trader = $game.MainModule.GetType('TraderScript')
        $general = $trader.Methods | Where-Object Name -eq 'GenerateSingleItemList'
        $generalStrings = @($general.Body.Instructions | Where-Object { $_.OpCode.Code.ToString() -eq 'Ldstr' -and $_.Operand -ne 'traderitemamount' } | ForEach-Object Operand)
        $special = $trader.Methods | Where-Object Name -eq 'GenerateInventory'
        $specialStrings = @($special.Body.Instructions | Where-Object { $_.OpCode.Code.ToString() -eq 'Ldstr' } | ForEach-Object Operand)
        if (($generalStrings -join '|') -ne ($rules['TraderCategories'] -join '|') -or
            ($specialStrings -join '|') -ne ($rules['WeaponTraderItems'] -join '|')) {
            throw 'Native trader selection rules differ from the current full-game code'
        }
        Write-Output 'PASS: compiled trader category and special-stock literals match the current full game'

        $unity = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path (Split-Path -Parent $GameAssemblyPath) 'UnityEngine.CoreModule.dll'))
        $resources = $unity.MainModule.GetType('UnityEngine.Resources')
        $loads = @($resources.Methods | Where-Object {
            $_.Name -eq 'LoadAsync' -and $_.IsPublic -and $_.IsStatic -and $_.GenericParameters.Count -eq 1 -and
            $_.ReturnType.FullName -eq 'UnityEngine.ResourceRequest' -and
            (($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -eq 'System.String'
        })
        if ($loads.Count -ne 1) { throw 'Unity Resources.LoadAsync<T>(string) contract mismatch' }
        $childReaders = @($unity.MainModule.GetType('UnityEngine.GameObject').Methods | Where-Object {
            $_.Name -eq 'GetComponentsInChildren' -and $_.IsPublic -and !$_.IsStatic -and $_.GenericParameters.Count -eq 1 -and
            $_.ReturnType.FullName -eq 'System.Void' -and
            (($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -eq 'System.Boolean,System.Collections.Generic.List`1<T>'
        })
        if ($childReaders.Count -ne 1) { throw 'Unity GameObject.GetComponentsInChildren<T>(bool, List<T>) contract mismatch' }
        Assert-Property $unity 'UnityEngine.ResourceRequest' 'asset' 'UnityEngine.Object'
        Assert-Property $unity 'UnityEngine.AsyncOperation' 'isDone' 'System.Boolean'
        Assert-Property $unity 'UnityEngine.Application' 'dataPath' 'System.String'
        Assert-Property $unity 'UnityEngine.Application' 'isFocused' 'System.Boolean'
        Assert-Property $unity 'UnityEngine.Camera' 'main' 'UnityEngine.Camera'
        Assert-InstanceMethod $unity 'UnityEngine.Camera' 'ScreenToWorldPoint' 'UnityEngine.Vector3' @('UnityEngine.Vector3')
        Assert-Property $unity 'UnityEngine.Screen' 'width' 'System.Int32'
        Assert-Property $unity 'UnityEngine.Screen' 'height' 'System.Int32'
        Assert-StaticMethod $unity 'UnityEngine.SceneManagement.SceneManager' 'GetActiveScene' 'UnityEngine.SceneManagement.Scene' @()
        Assert-Property $unity 'UnityEngine.SceneManagement.Scene' 'handle' 'System.Int32'
        foreach ($property in @(
            @('UnityEngine.Transform','localPosition','UnityEngine.Vector3'),
            @('UnityEngine.Transform','localRotation','UnityEngine.Quaternion'),
            @('UnityEngine.Transform','localScale','UnityEngine.Vector3'),
            @('UnityEngine.Transform','parent','UnityEngine.Transform'),
            @('UnityEngine.SpriteRenderer','sprite','UnityEngine.Sprite'),
            @('UnityEngine.SpriteRenderer','color','UnityEngine.Color'),
            @('UnityEngine.SpriteRenderer','flipX','System.Boolean'),
            @('UnityEngine.SpriteRenderer','flipY','System.Boolean'),
            @('UnityEngine.SpriteRenderer','drawMode','UnityEngine.SpriteDrawMode'),
            @('UnityEngine.SpriteRenderer','size','UnityEngine.Vector2'),
            @('UnityEngine.SpriteRenderer','tileMode','UnityEngine.SpriteTileMode'),
            @('UnityEngine.SpriteRenderer','adaptiveModeThreshold','System.Single'),
            @('UnityEngine.SpriteRenderer','spriteSortPoint','UnityEngine.SpriteSortPoint'),
            @('UnityEngine.SpriteRenderer','maskInteraction','UnityEngine.SpriteMaskInteraction'),
            @('UnityEngine.Renderer','sharedMaterial','UnityEngine.Material'),
            @('UnityEngine.Rendering.SortingGroup','sortingLayerID','System.Int32'),
            @('UnityEngine.Rendering.SortingGroup','sortingOrder','System.Int32'),
            @('UnityEngine.Rendering.SortingGroup','sortAtRoot','System.Boolean'),
            @('UnityEngine.SortingLayer','layers','UnityEngine.SortingLayer[]')
        )) { Assert-Property $unity $property[0] $property[1] $property[2] }
        Assert-Field $game 'TraderScript' 'eyes' 'UnityEngine.SpriteRenderer'
        Assert-Field $game 'TraderScript' 'headSprites' 'UnityEngine.Sprite[]'
        Assert-Field $game 'TraderScript' 'eyeSprites' 'UnityEngine.Sprite[]'
        $ghostType = $plugin.MainModule.GetType('UeiEntityGhost')
        $visualType = $plugin.MainModule.GetType('UeiEntityVisuals')
        if (!$ghostType -or !$visualType) { throw 'Compiled entity-shaped visual preview is missing' }
        $ghostBuild = $ghostType.Methods | Where-Object Name -eq 'Rebuild'
        $visualComponents = @($ghostBuild.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -in @('Call','Callvirt') -and $_.Operand.Name -eq 'AddComponent'
        } | ForEach-Object { $_.Operand.GenericArguments[0].FullName })
        if ($visualComponents -notcontains 'UnityEngine.SpriteRenderer' -or
            $visualComponents -notcontains 'UnityEngine.Rendering.SortingGroup' -or
            @($visualComponents | Where-Object { $_ -notin @('UnityEngine.SpriteRenderer','UnityEngine.Rendering.SortingGroup') }).Count -gt 0) {
            throw 'The ghost must create only visual components'
        }
        foreach ($type in @($ghostType,$visualType)) {
            foreach ($method in @($type.Methods | Where-Object HasBody)) {
                if ($method.Body.Instructions | Where-Object {
                    $_.OpCode.Code.ToString() -in @('Call','Callvirt') -and
                    ($_.Operand.Name -in @('Instantiate','SpawnEntity','SpawnResult') -or $_.Operand.DeclaringType.FullName -eq 'Utils')
                }) { throw 'Visual preview must not instantiate gameplay prefabs or spawn entities' }
            }
        }
        foreach ($type in @($visualType.NestedTypes | Where-Object { $_.Name -in @('Node','Part','Snapshot') })) {
            if ($type.Fields | Where-Object { $_.FieldType.FullName -in @('UnityEngine.GameObject','UnityEngine.Transform','UnityEngine.SpriteRenderer') }) {
                throw 'Cached visual data retains a gameplay prefab/component'
            }
        }
        $previewHint = $panelType.Methods | Where-Object Name -eq 'ShowPlacementPreview'
        if ($previewHint.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -in @('Call','Callvirt') -and $_.Operand.Name -in @('AddImage','AddIcon')
        }) { throw 'Placement must show the world appearance, not a crosshair or floating icon' }
        Write-Output 'PASS: composite sprite/pose/material ghost contracts, visual-only components and removed crosshair/icon'
        Assert-Property $unity 'UnityEngine.Texture' 'filterMode' 'UnityEngine.FilterMode'
        Assert-Property $unity 'UnityEngine.Texture' 'wrapMode' 'UnityEngine.TextureWrapMode'
        Assert-InstanceMethod $unity 'UnityEngine.Texture2D' 'SetPixels' 'System.Void' @('UnityEngine.Color[]')
        Assert-InstanceMethod $unity 'UnityEngine.Texture2D' 'Apply' 'System.Void' @('System.Boolean','System.Boolean')
        Assert-StaticMethod $unity 'UnityEngine.Sprite' 'Create' 'UnityEngine.Sprite' @('UnityEngine.Texture2D','UnityEngine.Rect','UnityEngine.Vector2','System.Single','System.UInt32','UnityEngine.SpriteMeshType')
        $missingIcon = $plugin.MainModule.GetType('UeiMissingIcon')
        if (!$missingIcon) { throw 'Cached missing-icon checkerboard implementation is absent' }
        $setIcon = $panelType.Methods | Where-Object Name -eq 'SetIcon'
        if (!($setIcon.Body.Instructions | Where-Object {
            $_.OpCode.Code.ToString() -in @('Call','Callvirt') -and $_.Operand.DeclaringType.FullName -eq 'UeiMissingIcon' -and $_.Operand.Name -eq 'get_Sprite'
        })) { throw 'UI missing icons do not use the shared checkerboard' }
        Write-Output 'PASS: cached display-only black/magenta fallback and native texture/sprite creation contracts'
        Write-Output 'PASS: full-game Unity asynchronous resource and child-component contracts'
    }
}
finally {
    $core.Dispose()
    $together.Dispose()
    $plugin.Dispose()
    if ($game) { $game.Dispose() }
    if ($unity) { $unity.Dispose() }
    if ($tmp) { $tmp.Dispose() }
}
