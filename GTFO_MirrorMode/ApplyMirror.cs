using UnityEngine;

namespace MirrorMode;

public class ApplyMirror : MonoBehaviour
{
    private static readonly Vector2 _scale = new Vector2(-1f, 1f);
    private static readonly Vector2 _offset = new Vector2(1f, 0f);
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Turns out I don't even need a custom material/shader for this :)
        Graphics.Blit(source, destination, _scale, _offset);
    }
}