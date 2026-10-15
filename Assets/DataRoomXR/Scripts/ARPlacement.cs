// Coloca el dashboard 3D sobre una superficie real en AR (celular Android con ARCore).
// Se compila SOLO si instalas AR Foundation + ARCore y defines USAR_AR_FOUNDATION
// (Project Settings ▸ Player ▸ Scripting Define Symbols). Ver README, sección "AR".
// Sin el define/paquete, este archivo no hace nada y el proyecto compila igual.
#if USAR_AR_FOUNDATION
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DataRoomXR
{
    /// <summary>
    /// Toca un plano detectado para posicionar el ContenedorGraficos ahí (AR móvil).
    /// Requiere en la escena: AR Session, XR Origin (con AR Plane Manager y AR Raycast Manager),
    /// y una AR Camera. Arrastra el ContenedorGraficos al campo 'contenedor'.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    public class ARPlacement : MonoBehaviour
    {
        [Tooltip("El objeto a colocar (ContenedorGraficos de la sala).")]
        public Transform contenedor;
        [Tooltip("Escala del dashboard en AR (más pequeño que en VR).")]
        public float escalaAR = 0.12f;
        public ARPlaneManager planeManager;

        ARRaycastManager raycaster;
        static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        bool colocado;

        void Awake()
        {
            raycaster = GetComponent<ARRaycastManager>();
            if (contenedor != null)
            {
                contenedor.localScale = Vector3.one * escalaAR;
                EstadoJugador.ModoVR = false;
            }
        }

        void Update()
        {
            if (Input.touchCount == 0) return;
            var toque = Input.GetTouch(0);
            if (toque.phase != TouchPhase.Began) return;

            if (raycaster.Raycast(toque.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = hits[0].pose;
                if (contenedor != null)
                {
                    contenedor.position = pose.position;
                    contenedor.rotation = Quaternion.Euler(0f, pose.rotation.eulerAngles.y, 0f);
                }
                colocado = true;
                // Al colocar, ocultamos los planos para que no estorben la vista.
                if (planeManager != null)
                    foreach (var p in planeManager.trackables) p.gameObject.SetActive(false);
            }
        }

        public bool Colocado => colocado;
    }
}
#endif
