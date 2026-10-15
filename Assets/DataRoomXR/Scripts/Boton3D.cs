using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Botón 3D (preguntas sugeridas, "preguntar", "modo AR/VR"). Se pulsa con clic/E en PC
    /// o mirándolo en VR. Avisa al DataRoomManager con su id y su texto.
    /// </summary>
    public class Boton3D : MonoBehaviour
    {
        public string id;
        public TextMesh etiqueta;
        public Renderer fondo;
        public Color color = new Color(0.18f, 0.22f, 0.3f);

        MaterialPropertyBlock mpb;
        bool resaltado;
        float tMirada;

        public bool Activo => gameObject.activeInHierarchy;
        public string Texto => etiqueta != null ? etiqueta.text : id;

        public void Configurar(string nuevoId, string texto)
        {
            id = nuevoId;
            if (etiqueta != null) etiqueta.text = texto;
            gameObject.SetActive(true);
            Pintar();
        }

        public void Ocultar() { resaltado = false; gameObject.SetActive(false); }

        void OnEnable() { Pintar(); }

        public void Resaltar(bool r) { resaltado = r; if (!r) tMirada = 0f; Pintar(); }

        /// <summary>Progreso de la mirada en VR (0..1) para animar el botón.</summary>
        public void Mirada(float t01) { tMirada = t01; }

        void Pintar()
        {
            if (fondo == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Color c = resaltado ? Color.Lerp(color, Color.white, 0.4f) : color;
            fondo.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            fondo.SetPropertyBlock(mpb);
            transform.localScale = resaltado ? Vector3.one * 1.05f : Vector3.one;
        }

        public void Usar()
        {
            if (!Activo || DataRoomManager.I == null) return;
            DataRoomManager.I.PulsarBoton(this);
        }
    }
}
