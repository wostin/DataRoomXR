using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Mano virtual con puntero láser visual para PC y VR.
    /// Crea una mano estilizada abajo a la derecha de la cámara y proyecta un láser cian
    /// hacia el punto de impacto de la mirada/apuntado.
    /// </summary>
    public class ManoPuntero : MonoBehaviour
    {
        LineRenderer laser;
        GameObject puntoImpacto;
        Transform puntaDedo;

        void Awake()
        {
            ConstruirMano();
            ConstruirLaser();
        }

        void ConstruirMano()
        {
            var mano = new GameObject("ManoVisual");
            mano.transform.SetParent(transform, false);
            mano.transform.localPosition = new Vector3(0.25f, -0.3f, 0.6f);
            mano.transform.localRotation = Quaternion.Euler(5f, -3f, 0f);

            var matPiel = CrearMaterial(new Color(1f, 0.8f, 0.65f), false);

            // Palma
            var palma = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palma.name = "Palma";
            palma.transform.SetParent(mano.transform, false);
            palma.transform.localPosition = Vector3.zero;
            palma.transform.localScale = new Vector3(0.12f, 0.03f, 0.15f);
            Destroy(palma.GetComponent<Collider>());
            palma.GetComponent<Renderer>().sharedMaterial = matPiel;

            // Dedo índice señalando al frente
            var dedo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dedo.name = "Dedo";
            dedo.transform.SetParent(mano.transform, false);
            dedo.transform.localPosition = new Vector3(0.035f, 0.005f, 0.135f);
            dedo.transform.localScale = new Vector3(0.03f, 0.03f, 0.12f);
            Destroy(dedo.GetComponent<Collider>());
            dedo.GetComponent<Renderer>().sharedMaterial = matPiel;

            // Punta del dedo (origen del láser)
            var punta = new GameObject("PuntaDedo");
            punta.transform.SetParent(dedo.transform, false);
            punta.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            puntaDedo = punta.transform;
        }

        void ConstruirLaser()
        {
            var matLaser = CrearMaterial(new Color(0f, 1f, 1f, 0.9f), true);

            var laserGo = new GameObject("Laser");
            laserGo.transform.SetParent(transform, false);
            laser = laserGo.AddComponent<LineRenderer>();
            laser.useWorldSpace = true;
            laser.positionCount = 2;
            laser.startWidth = 0.01f;
            laser.endWidth = 0.004f;
            laser.material = matLaser;
            laser.startColor = new Color(0f, 1f, 1f, 0.9f);
            laser.endColor = new Color(0.2f, 0.9f, 1f, 0.6f);

            puntoImpacto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puntoImpacto.name = "PuntoImpacto";
            puntoImpacto.transform.SetParent(transform, false);
            puntoImpacto.transform.localScale = Vector3.one * 0.05f;
            Destroy(puntoImpacto.GetComponent<Collider>());
            puntoImpacto.GetComponent<Renderer>().sharedMaterial = matLaser;
        }

        void Update()
        {
            Vector3 origen = puntaDedo != null ? puntaDedo.position : (transform.position + transform.right * 0.25f - transform.up * 0.3f + transform.forward * 0.6f);
            Ray ray = new Ray(transform.position, transform.forward);
            Vector3 destino;

            if (Physics.Raycast(ray, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                destino = hit.point;
                if (puntoImpacto != null)
                {
                    puntoImpacto.SetActive(true);
                    puntoImpacto.transform.position = hit.point;
                }
            }
            else
            {
                destino = transform.position + transform.forward * 6f;
                if (puntoImpacto != null)
                {
                    puntoImpacto.SetActive(false);
                }
            }

            if (laser != null)
            {
                laser.SetPosition(0, origen);
                laser.SetPosition(1, destino);
            }
        }

        static Material CrearMaterial(Color c, bool unlit)
        {
            Shader sh = null;
            if (unlit)
            {
                sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            }
            if (sh == null)
            {
                sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            }

            var m = new Material(sh);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }
    }
}
