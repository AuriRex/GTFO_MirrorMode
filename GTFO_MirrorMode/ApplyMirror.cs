using UnityEngine;

namespace MirrorMode;

public class ApplyMirror : MonoBehaviour
{
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, Plugin.MirrorMaterial);
    }
}