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
    
    internal static ManualLogSource L;

    private static readonly Harmony _harmony = new(GUID);

    private static ApplyMirror _gameMirrorApplier;
    
    public override void Load()
    {
        L = Log;

        ClassInjector.RegisterTypeInIl2Cpp<ApplyMirror>();
        
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        L.LogInfo("Plugin loaded!");
    }

    public static void SetGameMirror(bool active)
    {
        if (_gameMirrorApplier == null)
            return;
        
        _gameMirrorApplier.enabled = active;
    }

    public static void SetGUIRootMirrored(bool active)
    {
        GuiManager.Current.m_root.localScale = active ? Patches.INVERT_X : Vector3.one;
    }
    
    // internal static void ApplyShaderTo(CM_Camera camera)
    // {
    //     if (camera == null)
    //         return;
    //     
    //     TryLoadMaterial();
    //     
    //     var go = camera.Camera.gameObject;
    //     
    //     if (go.GetComponent<ApplyMirror>() == null)
    //         _menuMirrorApplier = go.AddComponent<ApplyMirror>();
    // }
    
    internal static void ApplyShaderTo(FPSCamera camera)
    {
        if (camera == null)
            return;
        
        //TryLoadMaterial();
        
        var go = camera.gameObject;
        
        if (go.GetComponent<ApplyMirror>() == null)
            _gameMirrorApplier = go.AddComponent<ApplyMirror>();
    }
}