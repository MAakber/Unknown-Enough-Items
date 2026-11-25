using System.Reflection;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

[assembly: AssemblyTitle(UeiPlugin.PluginName)]
[assembly: AssemblyProduct(UeiPlugin.PluginName)]
[assembly: AssemblyCompany(UeiPlugin.PluginAuthor)]
[assembly: AssemblyCopyright("Copyright (c) 2026 Aakber (小叶子)")]
[assembly: AssemblyVersion(UeiPlugin.PluginVersion)]
[assembly: AssemblyFileVersion(UeiPlugin.PluginVersion)]

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed partial class UeiPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "casualtiesunknown.uei";
    public const string PluginName = "UEI - Unknown Enough Items";
    public const string PluginVersionBase = "1.2";
#if !UEI_GENERATED_VERSION
    public const string PluginVersion = PluginVersionBase + ".10";
#endif
    public const string PluginAuthor = "Aakber (小叶子)";

    internal static UeiPlugin Instance = null!;
    internal static ConfigEntry<string> FavoriteEntries = null!;
    internal static ConfigEntry<string> LanguageMode = null!;
    internal static ConfigEntry<string> PanelPosition = null!;
    internal static ConfigEntry<float> PanelScale = null!;
    internal static ConfigEntry<int> DetailRowsPerPage = null!;
    internal static ConfigEntry<bool> CheatEnabled = null!;
    private static ConfigFile? ConfigFileRef;
    private static ManualLogSource? LogSourceRef;
    private static UeiRuntime? RuntimeRef;
    private static UeiProbeListener? ProbeRef;

    private Harmony? _harmony;

    private void Awake()
    {
        Logger.LogInfo("UEI plugin Awake.");
        Instance = this;
        ConfigFileRef = Config;
        LogSourceRef = Logger;
        UnityEngine.Object.DontDestroyOnLoad(gameObject);

        FavoriteEntries = Config.Bind("Favorites", "FavoriteEntries", string.Empty,
            "Favorited UEI entries, persisted as item:<id>|liquid:<id>.");
        UeiFavorites.Load(FavoriteEntries.Value);
        LanguageMode = Config.Bind("General", "Language", "auto",
            "UEI UI language: auto, en, zh.");
        PanelPosition = Config.Bind("General", "PanelPosition", "left",
            "UEI panel position: left or right.");
        PanelScale = Config.Bind("General", "PanelScale", 1.0f,
            "UEI panel scale multiplier. The in-panel setting cycles 0.85 - 1.30.");
        DetailRowsPerPage = Config.Bind("General", "DetailRowsPerPage", 2,
            "Card rows per detail page: 1 - 4 (default 2). Applies to acquisition, crafting and uses; denser pages scale card contents to fit.");
        CheatEnabled = Config.Bind("Cheat", "Enabled", false,
            "When enabled, left-clicking a legal UEI item gives one item to the player's inventory.");

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(UeiPlugin).Assembly);
        LogPatchStatus();

        EnsureRuntime("plugin Awake");
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void OnDisable()
    {
        Logger.LogInfo($"UEI plugin OnDisable. runtimeAlive={RuntimeRef != null} probeAlive={ProbeRef != null}");
    }

    private void OnDestroy()
    {
        Logger.LogInfo($"UEI plugin OnDestroy. runtimeAlive={RuntimeRef != null} probeAlive={ProbeRef != null}");
        Instance = null!;
    }

    internal static void EnsureRuntime(string source)
    {
        bool createdAnything = false;

        if (RuntimeRef == null)
        {
            GameObject go = new("UEI.Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            RuntimeRef = go.AddComponent<UeiRuntime>();
            LogInfo("Created standalone UEI runtime object.");
            createdAnything = true;
        }

        if (ProbeRef == null)
        {
            GameObject probe = new("UEI.Probe");
            UnityEngine.Object.DontDestroyOnLoad(probe);
            ProbeRef = probe.AddComponent<UeiProbeListener>();
            LogInfo("Created standalone UEI probe listener object.");
            createdAnything = true;
        }

        if (createdAnything)
        {
            LogInfo("UEI runtime ensured via " + source + ".");
        }
    }

    internal static void ClearRuntime(UeiRuntime runtime)
    {
        if (RuntimeRef == runtime)
        {
            RuntimeRef = null;
        }
    }

    internal static void ClearProbe(UeiProbeListener probe)
    {
        if (ProbeRef == probe)
        {
            ProbeRef = null;
        }
    }

    internal static void SaveFavorites()
    {
        if (FavoriteEntries == null)
        {
            return;
        }

        FavoriteEntries.Value = UeiFavorites.Serialize();
        ConfigFileRef?.Save();
    }

    internal static void SetLanguageMode(string mode)
    {
        if (LanguageMode == null)
        {
            return;
        }

        LanguageMode.Value = mode;
        ConfigFileRef?.Save();
    }

    internal static void SetPanelPosition(string position)
    {
        if (PanelPosition == null)
        {
            return;
        }

        PanelPosition.Value = position;
        ConfigFileRef?.Save();
    }

    internal static void SetPanelScale(float scale)
    {
        if (PanelScale == null)
        {
            return;
        }

        PanelScale.Value = Mathf.Clamp(scale, 0.85f, 1.3f);
        ConfigFileRef?.Save();
    }

    internal static void SetCheatEnabled(bool enabled)
    {
        if (CheatEnabled == null)
        {
            return;
        }

        CheatEnabled.Value = enabled;
        ConfigFileRef?.Save();
    }

    internal static int GetDetailRowsPerPage()
    {
        return Mathf.Clamp(DetailRowsPerPage?.Value ?? 2, 1, 4);
    }

    internal static void SetDetailRowsPerPage(int rows)
    {
        if (DetailRowsPerPage == null)
        {
            return;
        }

        DetailRowsPerPage.Value = Mathf.Clamp(rows, 1, 4);
        ConfigFileRef?.Save();
    }

    internal static void LogInfo(string message)
    {
        LogSourceRef?.LogInfo(message);
    }

    internal static void LogWarning(string message)
    {
        LogSourceRef?.LogWarning(message);
    }

    internal static void LogError(string message)
    {
        LogSourceRef?.LogError(message);
    }

    private void LogPatchStatus()
    {
        TryLogPatchStatus(typeof(PlayerCamera), "HandleInput");
        TryLogPatchStatus(typeof(PlayerCamera), "HandleRadialMenu");
        TryLogPatchStatus(typeof(PlayerCamera), "HandleWoundView");
        TryLogPatchStatus(typeof(GlobalDark), "Update");
    }

    private void TryLogPatchStatus(System.Type ownerType, string methodName)
    {
        try
        {
            MethodInfo? method = AccessTools.Method(ownerType, methodName);
            if (method == null)
            {
                Logger.LogWarning($"UEI patch target missing: {ownerType.Name}.{methodName}");
                return;
            }

            Patches? patches = Harmony.GetPatchInfo(method);
            string owners = patches == null
                ? "none"
                : string.Join(",",
                    patches.Prefixes.Select(x => "pre:" + x.owner)
                    .Concat(patches.Postfixes.Select(x => "post:" + x.owner))
                    .Concat(patches.Transpilers.Select(x => "trans:" + x.owner))
                    .Distinct());
            Logger.LogInfo($"UEI patch target {ownerType.Name}.{methodName} owners={owners}");
        }
        catch (System.Exception ex)
        {
            Logger.LogWarning($"UEI patch status failed for {ownerType.Name}.{methodName}: {ex.Message}");
        }
    }
}
