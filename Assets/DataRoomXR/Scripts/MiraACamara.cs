using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Billboard: gira unos textos para que siempre miren a la cámara (así se leen
    /// desde cualquier ángulo, igual en PC y en VR). Si 'soloEstos' está vacío, gira
    /// este propio objeto.
    /// </summary>
    public class MiraACamara : MonoBehaviour
    {
        public Transform[] soloEstos;

        Camera cam;

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null && EstadoJugador.Cabeza != null) cam = EstadoJugador.Cabeza.GetComponent<Camera>();
            Transform objetivo = EstadoJugador.Cabeza != null ? EstadoJugador.Cabeza : (cam != null ? cam.transform : null);
            if (objetivo == null) return;

            if (soloEstos != null && soloEstos.Length > 0)
            {
                foreach (var t in soloEstos)
                    if (t != null) t.rotation = Quaternion.LookRotation(t.position - objetivo.position);
            }
            else transform.rotation = Quaternion.LookRotation(transform.position - objetivo.position);
        }
    }
}
