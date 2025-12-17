using GameData;
using HarmonyLib;
using LevelGeneration;
using UnityEngine;

namespace MirrorMode;

public class Patches
{
    public static Vector3 INVERT_X = new Vector3(-1, 1, 1);
 
    private static Vector3 CompMult(Vector3 orig, Vector3 mult)
    {
        return new Vector3(orig.x * mult.x, orig.y * mult.y, orig.z * mult.z);
    }
    
    [HarmonyPatch(typeof(GameDataInit), nameof(GameDataInit.Initialize))]
    public static class InitPatch
    {
        private static bool _first = true;

        public static void Postfix()
        {
            if (!_first)
                return;
            
            _first = false;

            Plugin.OnGameInit();
        }
    }

    [HarmonyPatch(typeof(CM_Camera), nameof(CM_Camera.Awake))]
    public static class CM_Camera__Awake__Patch
    {
        public static void Postfix(CM_Camera __instance)
        {
            Plugin.ApplyShaderTo(__instance);
            Plugin.SetMenuMirror(false);
            Plugin.SetGUIRootMirrored(false);
        }
    }
    
    [HarmonyPatch(typeof(FPSCamera), nameof(FPSCamera.Setup), [])]
    public static class FPSCamera__Setup__Patch
    {
        public static void Postfix(FPSCamera __instance)
        {
            Plugin.ApplyShaderTo(__instance);
        }
    }
    
    // [HarmonyPatch(typeof(GuiManager), nameof(GuiManager.Setup))]
    // public static class GuiManager__Setup__Patch
    // {
    //     public static void Postfix(GuiManager __instance)
    //     {
    //         __instance.m_root.localScale = new Vector3(-1, 1, 1);
    //     }
    // }

    [HarmonyPatch(typeof(NavMarkerLayer), nameof(NavMarkerLayer.Setup), [typeof(Transform), typeof(string)])]
    public static class NavMarkerLayer__Setup__Patch
    {
        public static void Postfix(NavMarkerLayer __instance)
        {
            // Invert Nav Markers
            __instance.GuiLayerBase.transform.localScale = INVERT_X;
        }
    }

    [HarmonyPatch(typeof(FocusStateManager), nameof(FocusStateManager.ChangeState))]
    public static class FocusStateManager__ChangeState__Patch
    {
        public static void Postfix(FocusStateManager __instance, eFocusState state)
        {
            switch (state)
            {
                case eFocusState.FPS:
                case eFocusState.FPS_CommunicationDialog:
                case eFocusState.InElevator:
                case eFocusState.ComputerTerminal:
                    Plugin.SetGUIRootMirrored(true);
                    // Idk, the setup patch doesn't seem to work >->
                    GuiManager.NavMarkerLayer.GuiLayerBase.transform.localScale = INVERT_X;
                    break;
                default:
                    Plugin.SetGUIRootMirrored(false);
                    GuiManager.NavMarkerLayer.GuiLayerBase.transform.localScale = Vector3.one;
                    break;
            }
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
            
            serial.localPosition = CompMult(serial.localPosition, INVERT_X);
            serial.localScale = CompMult(serial.localScale, INVERT_X);
        }
    }
    
    // [HarmonyPatch(typeof(GUIX_VirtualScene), nameof(GUIX_VirtualScene.Start))]
    // public static class GUIX_VirtualScene__Start__Patch
    // {
    //     public static void Postfix(GUIX_VirtualScene __instance)
    //     {
    //         if (!__instance.name.ToLower().Contains("terminal"))
    //             return;
    //         
    //         __instance.gameObject.transform.localScale = new Vector3(-1, 1, 1);
    //         __instance.gameObject.transform.localPosition = new Vector3(-2.725f, 0, 0);
    //     }
    // }

    [HarmonyPatch(typeof(LG_Sign), nameof(LG_Sign.SetZoneInfo))]
    public static class LG_Sign__SetZoneInfo__Patch
    {
        public static void Postfix(LG_Sign __instance)
        {
            __instance.m_text.transform.localScale = CompMult(__instance.m_text.transform.localScale, INVERT_X);
            //__instance.transform.localScale = INVERT_X;
        }
    }
    
    [HarmonyPatch(typeof(LG_Door_Sync), nameof(LG_Door_Sync.Setup))]
    public static class DoorPatch
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
                    box.transform.localScale = CompMult(box.transform.localScale, INVERT_X);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(InputMapper), nameof(InputMapper.DoGetAxis))]
    public static class InputMapperPatch
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
                case eFocusState.Map:
                    break;
                default:
                    return;
            }
            
            __result *= -1;
        }
    }
    
    
    /*
     *
    public void SetLeftArmTargetPosRot(Transform targetTrans)
	{
		if (targetTrans != null)
		{
			this.m_tempRot = targetTrans.rotation;
			this.m_tempRot *= Quaternion.Euler(this.m_leftHandIKRotOffset);
			this.m_leftHandIKTarget.SetPositionAndRotation(targetTrans.position + targetTrans.right * this.m_leftHandIKPosOffset.x + targetTrans.up * this.m_leftHandIKPosOffset.y + targetTrans.forward * this.m_leftHandIKPosOffset.z, this.m_tempRot);
		}
	}

	// Token: 0x060006D4 RID: 1748 RVA: 0x0006090C File Offset: 0x0005EB0C
	public void SetRightArmTargetPosRot(Transform targetTrans)
	{
		if (targetTrans != null)
		{
			this.m_tempRot = targetTrans.rotation;
			this.m_tempRot *= Quaternion.Euler(this.m_rightHandIKRotOffset);
			this.m_rightHandIKTarget.SetPositionAndRotation(targetTrans.position + targetTrans.right * this.m_rightHandIKPosOffset.x + targetTrans.up * this.m_rightHandIKPosOffset.y + targetTrans.forward * this.m_rightHandIKPosOffset.z, this.m_tempRot);
		}
	}
     * 
     */

    #region LEFT_HANDED_MODE_VIEWMODEL
    [HarmonyPatch(typeof(FirstPersonItemHolder), nameof(FirstPersonItemHolder.Setup))]
    public static class FirstPersonItemHolder__Setup__Patch
    {
        public static void Postfix(FirstPersonItemHolder __instance)
        {
            __instance.transform.localScale = INVERT_X;
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