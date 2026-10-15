using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Mejora de ambiente AUTOCONTENIDA (no modifica otros scripts ni la escena).
    /// Se activa sola al iniciar el juego (RuntimeInitializeOnLoadMethod) y añade:
    ///  - Una luz de relleno azulada suave para dar profundidad a la sala.
    ///  - "Polvo de datos": partículas finas flotando lentamente en el aire.
    /// Todo en runtime; no ensucia la escena guardada.
    /// </summary>
    public static class MejorasAmbiente
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            var go = new GameObject("=== AMBIENTE (runtime) ===");
            go.AddComponent<MejorasAmbienteRunner>();
        }
    }

    public class MejorasAmbienteRunner : MonoBehaviour
    {
        void Start()
        {
            // Luz de relleno azulada
            var lg = new GameObject("LuzRelleno");
            lg.transform.SetParent(transform);
            lg.transform.position = new Vector3(0f, 6f, 9f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 90f;
            l.intensity = 0.55f;
            l.color = new Color(0.45f, 0.65f, 1f);
            RenderSettings.ambientLight = new Color(0.40f, 0.43f, 0.52f);

            // Polvo de datos: partículas flotantes
            var pg = new GameObject("PolvoDeDatos");
            pg.transform.SetParent(transform);
            pg.transform.position = new Vector3(0f, 3f, 6f);
            var ps = pg.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.startColor = new Color(0.6f, 0.8f, 1f, 0.45f);
            main.startSize = 0.045f;
            main.startSpeed = 0.18f;
            main.startLifetime = 9f;
            main.maxParticles = 400;
            main.gravityModifier = -0.01f; // flotan muy lento hacia arriba
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 28f;

            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(34f, 8f, 22f);

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Particles/Standard Unlit");
            if (shader != null) rend.material = new Material(shader);
            rend.sortingOrder = -1;

            ps.Play();
        }
    }
}
