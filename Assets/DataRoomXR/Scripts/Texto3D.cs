using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Mantiene la textura de la fuente en el material del texto 3D
    /// (la fuente dinámica se regenera al aparecer letras nuevas).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TextMesh))]
    public class Texto3D : MonoBehaviour
    {
        void OnEnable() { Font.textureRebuilt += Reconstruida; Aplicar(); }
        void OnDisable() { Font.textureRebuilt -= Reconstruida; }
        void Reconstruida(Font f) { Aplicar(); }

        void Aplicar()
        {
            var tm = GetComponent<TextMesh>();
            var r = GetComponent<MeshRenderer>();
            if (tm == null || r == null || tm.font == null || r.sharedMaterial == null) return;
            if (r.sharedMaterial.mainTexture != tm.font.material.mainTexture)
                r.sharedMaterial.mainTexture = tm.font.material.mainTexture;
        }
    }
}
