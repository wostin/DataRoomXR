using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Jugador para PC: WASD mover, mouse mirar, clic/E para seleccionar una barra,
    /// Tab abre el panel de preguntas, Esc libera el cursor.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class JugadorFPS : MonoBehaviour
    {
        public Transform cabeza;
        public Camera camara;
        public float velocidad = 2.8f;
        public float sensibilidad = 2f;
        public float alturaOjos = 1.6f;
        public float alcance = 30f;

        CharacterController cc;
        float pitch, vy;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (camara == null) camara = GetComponentInChildren<Camera>();
            if (cabeza == null) cabeza = camara.transform.parent != null ? camara.transform.parent : camara.transform;
            EstadoJugador.Raiz = transform;
            EstadoJugador.Cabeza = camara.transform;
            EstadoJugador.ModoVR = false;
        }

        void Update()
        {
            if (EstadoJugador.Bloqueado || Cursor.lockState != CursorLockMode.Locked) { Marcar(null); return; }

            Vector2 m = Entrada.Mirar() * sensibilidad;
            transform.Rotate(0f, m.x, 0f);
            pitch = Mathf.Clamp(pitch - m.y, -80f, 80f);
            cabeza.localEulerAngles = new Vector3(pitch, 0f, 0f);
            cabeza.localPosition = new Vector3(0f, alturaOjos, 0f);

            Vector2 mv = Entrada.Mover();
            Vector3 dir = transform.right * mv.x + transform.forward * mv.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            vy = cc.isGrounded ? -1f : vy - 9.8f * Time.deltaTime;
            cc.Move((dir * velocidad + Vector3.up * vy) * Time.deltaTime);

            BarraDato b = null; Boton3D boton = null;
            if (Physics.Raycast(camara.transform.position, camara.transform.forward, out RaycastHit hit, alcance, ~0, QueryTriggerInteraction.Ignore))
            {
                boton = hit.collider.GetComponentInParent<Boton3D>();
                if (boton == null) b = hit.collider.GetComponentInParent<BarraDato>();
            }
            Marcar(b);
            MarcarBoton(boton);
            if (Entrada.Clic())
            {
                if (boton != null) boton.Usar();
                else if (b != null && DataRoomManager.I != null) DataRoomManager.I.SeleccionarBarra(b);
            }
        }

        void Marcar(BarraDato b)
        {
            if (EstadoJugador.Apuntada == b) return;
            if (EstadoJugador.Apuntada != null) EstadoJugador.Apuntada.Resaltar(false);
            EstadoJugador.Apuntada = b;
            if (b != null) b.Resaltar(true);
        }

        Boton3D botonApuntado;
        void MarcarBoton(Boton3D b)
        {
            if (botonApuntado == b) return;
            if (botonApuntado != null) botonApuntado.Resaltar(false);
            botonApuntado = b;
            if (b != null) b.Resaltar(true);
        }
    }
}
