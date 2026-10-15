using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Mejora AUTOCONTENIDA: dibuja anillos de luz concéntricos que laten en el piso,
    /// bajo la zona del gráfico, para dar un aire de "plataforma de datos" futurista.
    /// Se activa sola al iniciar; no modifica otros scripts ni la escena guardada.
    /// </summary>
    public static class PlataformaDatos
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            var go = new GameObject("=== PLATAFORMA (runtime) ===");
            go.AddComponent<PlataformaDatosRunner>();
        }
    }

    public class PlataformaDatosRunner : MonoBehaviour
    {
        readonly float[] radios = { 3.2f, 5.2f, 7.4f };
        LineRenderer[] anillos;
        Material mat;
        static readonly Vector3 CENTRO = new Vector3(0f, 0.05f, 6f);
        const int PUNTOS = 72;

        void Start()
        {
            var shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");
            mat = new Material(shader);
            mat.color = new Color(0.25f, 0.8f, 1f, 0.9f);

            anillos = new LineRenderer[radios.Length];
            for (int i = 0; i < radios.Length; i++)
            {
                var g = new GameObject("Anillo" + i);
                g.transform.SetParent(transform);
                var lr = g.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.loop = true;
                lr.positionCount = PUNTOS;
                lr.widthMultiplier = 0.05f;
                lr.material = mat;
                lr.numCapVertices = 2;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;

                for (int p = 0; p < PUNTOS; p++)
                {
                    float a = (p / (float)PUNTOS) * Mathf.PI * 2f;
                    lr.SetPosition(p, CENTRO + new Vector3(Mathf.Cos(a) * radios[i], 0f, Mathf.Sin(a) * radios[i]));
                }
                anillos[i] = lr;
            }
        }

        void Update()
        {
            // Latido suave: el grosor y el brillo de cada anillo pulsan desfasados.
            for (int i = 0; i < anillos.Length; i++)
            {
                float t = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.6f - i * 0.8f);
                anillos[i].widthMultiplier = Mathf.Lerp(0.02f, 0.07f, t);
                var c = new Color(0.25f, 0.8f, 1f, Mathf.Lerp(0.25f, 0.9f, t));
                anillos[i].startColor = c;
                anillos[i].endColor = c;
            }
        }
    }
}
