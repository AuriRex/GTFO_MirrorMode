using CellMenu;
using GameData;
using Gear;
using HarmonyLib;
using LevelGeneration;
using Player;
using SNetwork;
using UnityEngine;

namespace MirrorMode;

public static class Patches
{
    public static Vector3 INVERT_X = new Vector3(-1, 1, 1);
    
    [HarmonyPatch(typeof(FPSCamera), nameof(FPSCamera.Setup), [])]
    public static class FPSCamera__Setup__Patch
    {
        public static void Postfix(FPSCamera __instance)
        {
            Plugin.ApplyShaderTo(__instance);
        }
    }

    [HarmonyPatch(typeof(CM_PageObjectives), nameof(CM_PageObjectives.OnEnable))]
    public static class CM_PageObjectives__OnEnable__Patch
    {
        private static bool _first = true;
        public static void Postfix(CM_PageObjectives __instance)
        {
            if (_first)
            {
                _first = false;
                return;
            }
            
            Plugin.SetGUIRootMirrored(true);
            GuiManager.WatermarkLayer.CanvasTrans.localScale = Vector3.one;
        }
    }
    
    [HarmonyPatch(typeof(CM_PageMap), nameof(CM_PageMap.OnEnable))]
    public static class CM_PageMap__OnEnable__Patch
    {
        private static bool _first = true;
        public static void Postfix(CM_PageMap __instance)
        {
            if (_first)
            {
                _first = false;
                return;
            }
            
            Plugin.SetGUIRootMirrored(false);
            GuiManager.WatermarkLayer.CanvasTrans.localScale = INVERT_X;
        }
    }
    
    [HarmonyPatch(typeof(FocusStateManager), nameof(FocusStateManager.ChangeState))]
    public static class FocusStateManager__ChangeState__Patch
    {
        public static void Postfix(FocusStateManager __instance, eFocusState state)
        {
            GuiManager.WatermarkLayer.CanvasTrans.localScale = Vector3.one;
            
            switch (state)
            {
                case eFocusState.FPS:
                case eFocusState.FPS_CommunicationDialog:
                case eFocusState.FPS_TypingInChat:
                case eFocusState.InElevator:
                case eFocusState.ComputerTerminal:
                    Plugin.SetGUIRootMirrored(true);
                    break;
                case eFocusState.Map:
                    var map = CM_PageMap.Current;
                    map.m_cursor.transform.localScale = INVERT_X;
                    map.m_staticContentHolder.localScale = INVERT_X;
                    foreach (var syncedPlayer in map.m_syncedPlayers)
                    {
                        // shrug
                        syncedPlayer.transform.localScale = INVERT_X;
                    }
                    if (CM_PageMap.Current.isActiveAndEnabled)
                        GuiManager.WatermarkLayer.CanvasTrans.localScale = INVERT_X;
                    break;
                default:
                    Plugin.SetGUIRootMirrored(false);
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(EnemyScannerGraphics), nameof(EnemyScannerGraphics.Start))]
    public static class EnemyScannerGraphics__Start__Patch
    {
        // Flip bio-tracker dots display
        public static void Postfix(EnemyScannerGraphics __instance)
        {
            var displayTrans = __instance.m_display.transform;

            if (displayTrans.localScale.x < 0)
                return;
            
            displayTrans.localScale = Vector3.Scale(displayTrans.localScale, INVERT_X);
        }
    }
    
    [HarmonyPatch(typeof(SentryGunScreen), nameof(SentryGunScreen.SetAmmo))]
    public static class SentryGunScreen__SetAmmo__Patch
    {
        public static void Postfix(SentryGunScreen __instance)
        {
            if (__instance == null || __instance.transform == null)
                return;

            if (__instance.GetComponentInParent<SentryGunFirstPerson>() != null)
            {
                __instance.transform.localScale = Vector3.one;
                return;
            }
            
            __instance.transform.localScale = INVERT_X;
        }
    }
    
    [HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.DoChangeState))]
    public static class GameStateManager__DoChangeState__Patch
    {
        public static void Postfix(GameStateManager __instance, eGameStateName nextState)
        {
            if (nextState != eGameStateName.InLevel)
                return;
            
            FlipMapIcons();
        }

        private static void FlipMapIcons()
        {
            var map = CM_PageMap.Current;

            var mapMoverElementsRootTrans = CM_PageMap.m_mapMoverElementsRoot?.transform;

            if (mapMoverElementsRootTrans != null)
            {
                for (var i = 0; i < mapMoverElementsRootTrans.childCount; i++)
                {
                    var child =  mapMoverElementsRootTrans.GetChild(i);
                    
                    // Dimensions have their own 'E' it seems :p
                    if (!child.name.StartsWith("CM_MapElevator(Clone)"))
                        continue;
                    
                    child.localScale = INVERT_X;
                }
            }
            
            foreach (var zoneGui in map.m_zoneGUI)
            {
                foreach (var areaGui in zoneGui.m_areaGUIs)
                {
                    FlipGuiItems(areaGui.m_signGUIs);
                    FlipGuiItems(areaGui.m_computerTerminalGUIs);
                    FlipGuiItems(areaGui.m_doorGUIs);
                    FlipGuiItems(areaGui.m_bulkheadDoorControllerGUIs);
                }
            }

            return;

            void FlipGuiItems(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<CM_SyncedGUIItem> items)
            {
                foreach (var item in items)
                {
                    item.transform.localScale = Vector3.Scale(item.transform.localScale, INVERT_X);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(GuiManager), nameof(GuiManager.ScreenToGUIScaled))]
    public static class GuiManager__ScreenToGUIScaled__Patch
    {
        public static void Postfix(GuiManager __instance, ref Vector3 __result)
        {
            switch (FocusStateManager.CurrentState)
            {
                case eFocusState.FPS:
                case eFocusState.FPS_CommunicationDialog:
                case eFocusState.FPS_TypingInChat:
                case eFocusState.InElevator:
                case eFocusState.Map:
                    break;
                default:
                    return;
            }
            
            __result.x *= -1;
        }
    }
    
    [HarmonyPatch(typeof(AkGameObj), nameof(AkGameObj.Update))]
    public static class AkGameObj__Update__Patch
    {
        private const string GAMEOBJECT_NAME_FILTER = "FPSLookCamera";
        
        // This does seem to work to properly flip the audio by essentially
        // flipping the player backwards as much as the audio engine is concerned,
        // although I am unsure if this has any noticeable impact on audio ...
        public static void Postfix(AkGameObj __instance)
        {
            if (__instance.name != GAMEOBJECT_NAME_FILTER)
                return;

            var transform = __instance.transform;
            var position = transform.position;
            var forward = transform.forward * -1;
            
            AkSoundEngine.SetObjectPosition(__instance.gameObject,
                position.x, position.y, position.z,
                forward.x, forward.y, forward.z,
                transform.up.x, transform.up.y, transform.up.z);
        }
    }
    
    [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup))]
    public static class LG_ComputerTerminal__Setup__Patch
    {
        public static void Postfix(LG_ComputerTerminal __instance)
        {
            var graphics = __instance.transform.Find("Graphics");
            
            if (graphics != null)
                graphics.transform.localScale = INVERT_X;

            var serial = __instance.m_serial.transform;
            
            serial.localPosition = Vector3.Scale(serial.localPosition, INVERT_X);
            serial.localScale = Vector3.Scale(serial.localScale, INVERT_X);
        }
    }

    [HarmonyPatch(typeof(LG_Sign), nameof(LG_Sign.SetZoneInfo))]
    public static class LG_Sign__SetZoneInfo__Patch
    {
        public static void Postfix(LG_Sign __instance)
        {
            __instance.m_text.transform.localScale = Vector3.Scale(__instance.m_text.transform.localScale, INVERT_X);
        }
    }
    
    [HarmonyPatch(typeof(LG_Door_Sync), nameof(LG_Door_Sync.Setup))]
    public static class LG_Door_Sync__Setup__Patch
    {
        public static void Postfix(LG_Door_Sync __instance)
        {
            var secDoor = __instance.m_core.TryCast<LG_SecurityDoor>();

            if (secDoor != null)
            {
                secDoor.transform.localScale = INVERT_X;
                return;
            }

            var weakDoor = __instance.m_core.TryCast<LG_WeakDoor>();

            if (weakDoor != null)
            {
                weakDoor.m_doorBladeCuller.transform.localScale = INVERT_X;
                var boxes = weakDoor.m_doorBladeCuller.GetComponentsInChildren<BoxCollider>();

                foreach (var box in boxes)
                {
                    box.transform.localScale = Vector3.Scale(box.transform.localScale, INVERT_X);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(InputMapper), nameof(InputMapper.DoGetAxis))]
    public static class InputMapper__DoGetAxis__Patch
    {
        public static void Postfix(InputMapper __instance, InputAction action, ref float __result)
        {
            if (action != InputAction.MoveHorizontal && action != InputAction.LookHorizontal)
                return;

            switch (FocusStateManager.CurrentState)
            {
                case eFocusState.FPS:
                case eFocusState.FPS_CommunicationDialog:
                case eFocusState.InElevator:
                    break;
                case eFocusState.Map:
                    // FocusState Map but map not enabled -> objectives screen
                    if (!CM_PageMap.Current.isActiveAndEnabled)
                        return;
                    break;
                default:
                    return;
            }
            
            __result *= -1;
        }
    }

    #region LEFT_HANDED_MODE_VIEWMODEL
    [HarmonyPatch(typeof(FirstPersonItemHolder), nameof(FirstPersonItemHolder.Setup))]
    public static class FirstPersonItemHolder__Setup__Patch
    {
        public static void Postfix(FirstPersonItemHolder __instance)
        {
            __instance.transform.localScale = INVERT_X;
            
            // We have to reset the scale on our items/weapons or else
            // the IK breaks for our first-person hands
            // The FPIH gets destroyed with the actual weapons inheriting the -1 scale
            // on the X axis after being un-parented from the FPIH
            var bp = PlayerBackpackManager.GetBackpack(SNet.LocalPlayer);

            foreach (var bpItem in bp.BackpackItems)
            {
                if (bpItem.Instance == null)
                    continue;
                
                //Plugin.L.LogWarning($"Setting scale of {bpItem.Instance.name} ...");
                bpItem.Instance.transform.localScale = Vector3.one;
            }
        }
    }
    
    [HarmonyPatch(typeof(PlayerFPSBody), nameof(PlayerFPSBody.SetLeftArmTargetPosRot))]
    public static class PlayerFPSBody__SetLeftArmTargetPosRot__Patch
    {
        public static bool Prefix(PlayerFPSBody __instance, Transform targetTrans)
        {
            if (targetTrans == null)
                return false;
            
            __instance.m_tempRot = targetTrans.rotation;
            __instance.m_tempRot *= Quaternion.Euler(__instance.m_leftHandIKRotOffset);
            __instance.m_leftHandIKTarget.SetPositionAndRotation(targetTrans.position + (-1 * targetTrans.right) * __instance.m_leftHandIKPosOffset.x + targetTrans.up * __instance.m_leftHandIKPosOffset.y + targetTrans.forward * __instance.m_leftHandIKPosOffset.z, __instance.m_tempRot);
            return false;
        }
    }
    
    [HarmonyPatch(typeof(PlayerFPSBody), nameof(PlayerFPSBody.SetRightArmTargetPosRot))]
    public static class PlayerFPSBody__SetRightArmTargetPosRot__Patch
    {
        public static bool Prefix(PlayerFPSBody __instance, Transform targetTrans)
        {
            if (targetTrans == null)
                return false;
            
            __instance.m_tempRot = targetTrans.rotation;
            __instance.m_tempRot *= Quaternion.Euler(__instance.m_rightHandIKRotOffset);
            __instance.m_rightHandIKTarget.SetPositionAndRotation(targetTrans.position + (-1 * targetTrans.right) * __instance.m_rightHandIKPosOffset.x + targetTrans.up * __instance.m_rightHandIKPosOffset.y + targetTrans.forward * __instance.m_rightHandIKPosOffset.z, __instance.m_tempRot);
            return false;
        }
    }
    #endregion LEFT_HANDED_MODE_VIEWMODEL
}