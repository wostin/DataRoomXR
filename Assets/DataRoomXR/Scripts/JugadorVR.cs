using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Adaptador para VR (celular + visor Cardboard, o visor OpenXR). Se coloca en el "XR Origin".
    /// Seleccionas una barra MIRÁNDOLA; el panel de preguntas se controla por voz o por las
    /// preguntas sugeridas (botones 3D). No referencia paquetes XR: compila sin ellos.
    /// </summary>
    public class JugadorVR : MonoBehaviour
    {
        public Camera camaraVR;
        public float tiempoMirada = 1.0f;
        public float alcance = 30f;

        BarraDato mirada;
        Boton3D botonMirado;
        float t, tBoton;
        Transform puntero;

        void Awake()
        {
            if (camaraVR == null) camaraVR = GetComponentInChildren<Camera>();
            EstadoJugador.Raiz = transform;
            EstadoJugador.Cabeza = camaraVR.transform;
            EstadoJugador.ModoVR = true;

#if UNITY_2023_1_OR_NEWER
            var fps = FindAnyObjectByType<JugadorFPS>();
#else
            var fps = FindObjectOfType<JugadorFPS>();
#endif
            if (fps != null) fps.gameObject.SetActive(false);

            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(p.GetComponent<Collider>());
            p.name = "PunteroMirada";
            p.transform.SetParent(camaraVR.transform, false);
            p.transform.localPosition = new Vector3(0f, 0f, 1f);
            p.transform.localScale = Vector3.one * 0.012f;
            var r = p.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            r.material.color = Color.white;
            puntero = p.transform;
        }

        void Update()
        {
            BarraDato actual = null; Boton3D boton = null;
            var c = camaraVR.transform;
            if (!EstadoJugador.Bloqueado &&
                Physics.Raycast(c.position, c.forward, out RaycastHit hit, alcance, ~0, QueryTriggerInteraction.Ignore))
            {
                boton = hit.collider.GetComponentInParent<Boton3D>();
                if (boton == null) actual = hit.collider.GetComponentInParent<BarraDato>();
            }

            // Botones por mirada
            if (boton != botonMirado)
            {
                if (botonMirado != null) botonMirado.Resaltar(false);
                botonMirado = boton; tBoton = 0f;
                if (botonMirado != null) botonMirado.Resaltar(true);
            }
            if (botonMirado != null)
            {
                tBoton += Time.deltaTime;
                botonMirado.Mirada(Mathf.Clamp01(tBoton / tiempoMirada));
                if (tBoton >= tiempoMirada) { botonMirado.Usar(); botonMirado.Resaltar(false); botonMirado = null; tBoton = 0f; }
            }

            // Barras por mirada
            if (actual != mirada)
            {
                if (mirada != null) mirada.Resaltar(false);
                mirada = actual; t = 0f;
                if (mirada != null) mirada.Resaltar(true);
                EstadoJugador.Apuntada = mirada;
            }
            if (mirada != null)
            {
                t += Time.deltaTime;
                if (t >= tiempoMirada && DataRoomManager.I != null) { DataRoomManager.I.SeleccionarBarra(mirada); t = 0f; }
            }
            float prog = botonMirado != null ? tBoton / tiempoMirada : (mirada != null ? t / tiempoMirada : 0f);
            puntero.localScale = Vector3.one * (0.012f + 0.03f * Mathf.Clamp01(prog));
        }
    }
}
