using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using MirrorMode;
using UnityEngine;

[assembly: AssemblyVersion(Plugin.VERSION)]
[assembly: AssemblyFileVersion(Plugin.VERSION)]
[assembly: AssemblyInformationalVersion(Plugin.VERSION)]

namespace MirrorMode;

[BepInPlugin(GUID, MOD_NAME, VERSION)]
public class Plugin : BasePlugin
{
    public const string GUID = "dev.aurirex.gtfo.mirrormode";
    public const string MOD_NAME = ManifestInfo.TSName;
    public const string VERSION = ManifestInfo.TSVersion;

    public const string MIRROR_MODE_ASSET_PATH = "assets/mirror_mode/hidden_mirrormode.mat";
    
    internal static ManualLogSource L;

    private static readonly Harmony _harmony = new(GUID);

    private static ApplyMirror _gameMirrorApplier;
    private static ApplyMirror _menuMirrorApplier;
    
    public static Material MirrorMaterial { get; private set; }
    
    public override void Load()
    {
        L = Log;

        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        L.LogInfo("Plugin loaded!");
        
        ClassInjector.RegisterTypeInIl2Cpp<ApplyMirror>();
    }

    public static void OnGameInit()
    {
        TryLoadMaterial();
    }

    public static void SetGameMirror(bool active)
    {
        if (_gameMirrorApplier == null)
            return;
        
        _gameMirrorApplier.enabled = active;
    }
    
    public static void SetMenuMirror(bool active)
    {
        if (_menuMirrorApplier == null)
            return;
        
        _menuMirrorApplier.enabled = active;
    }

    public static void SetGUIRootMirrored(bool active)
    {
        GuiManager.Current.m_root.localScale = new Vector3(active ? -1 : 1, 1, 1);
    }
    
    private static void TryLoadMaterial()
    {
        var mat = MirrorMaterial;
        
        if (mat != null)
            return;
        
        var bundle = AssetBundle.LoadFromMemory(Resources.Data.mirrormode_shader);
        mat = bundle.LoadAsset(MIRROR_MODE_ASSET_PATH).Cast<Material>();
        bundle.Unload(unloadAllLoadedObjects: false);
        
        UnityEngine.Object.DontDestroyOnLoad(mat);
        mat.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;

        MirrorMaterial = mat;
    }
    
    internal static void ApplyShaderTo(CM_Camera camera)
    {
        if (camera == null)
            return;
        
        TryLoadMaterial();
        
        var go = camera.Camera.gameObject;
        
        if (go.GetComponent<ApplyMirror>() == null)
            _menuMirrorApplier = go.AddComponent<ApplyMirror>();
    }
    
    internal static void ApplyShaderTo(FPSCamera camera)
    {
        if (camera == null)
            return;
        
        TryLoadMaterial();
        
        var go = camera.gameObject;
        
        if (go.GetComponent<ApplyMirror>() == null)
            _gameMirrorApplier = go.AddComponent<ApplyMirror>();
    }
}