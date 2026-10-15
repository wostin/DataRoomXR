using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// En modo demo (autoDemo = true), orbita lentamente alrededor del gráfico
    /// para mostrarlo de forma cinemática. Al desactivarse la demo, el jugador recupera el control.
    /// </summary>
    public class CamaraOrbital : MonoBehaviour
    {
        public Vector3 centro = new Vector3(0f, 2f, 6f);
        public float radio = 9f;
        public float altura = 3f;
        public float velocidad = 8f; // grados por segundo

        void LateUpdate()
        {
            if (DataRoomManager.I == null || !DataRoomManager.I.autoDemo) return;
            if (DataRoomManager.I.mostrandoMenu) return;

            Transform raiz = EstadoJugador.Raiz != null ? EstadoJugador.Raiz : transform;
            if (raiz == null) return;

            float angulo = (Time.time * velocidad) * Mathf.Deg2Rad;
            float x = centro.x + Mathf.Cos(angulo) * radio;
            float z = centro.z + Mathf.Sin(angulo) * radio;
            float y = centro.y + altura;

            raiz.position = new Vector3(x, y, z);
            raiz.LookAt(centro);

            if (EstadoJugador.Cabeza != null && EstadoJugador.Cabeza != raiz)
            {
                EstadoJugador.Cabeza.localRotation = Quaternion.identity;
            }
        }
    }
}
