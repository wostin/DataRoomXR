#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Arma toda la escena de DataRoom XR con un clic:
    /// menú  Tools ▸ DataRoom XR ▸ ★ ARMAR SALA DE DATOS.
    /// Crea la sala, el jugador de PC, el rig de VR (desactivado), los 3 gráficos (barras, líneas,
    /// mapa de Junín), el panel de preguntas con sugeridas + historial + botones de control,
    /// los paneles de detalle e insight, la fuente de datos en memoria, luz/skybox y el audio,
    /// y enlaza todas las referencias de los scripts. No necesitas tocar el Inspector.
    /// </summary>
    public static class DataRoomSetup
    {
        const string RAIZ = "=== DATAROOM XR ===";
        static Font Fuente => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Material matBarra, matPanel, matBoton, matGrilla, matLinea, matMapa;

        [MenuItem("Tools/DataRoom XR/★ ARMAR SALA DE DATOS", false, 0)]
        public static void Armar()
        {
            var vieja = GameObject.Find(RAIZ);
            if (vieja != null) Object.DestroyImmediate(vieja);

            // Quita la cámara y la luz por defecto de la escena vacía, si existen, para que
            // solo renderice la cámara del jugador (si no, se ven dos cámaras superpuestas).
            foreach (var n in new[] { "Main Camera", "Directional Light" })
            {
                var o = GameObject.Find(n);
                if (o != null) Object.DestroyImmediate(o);
            }

            var raiz = new GameObject(RAIZ).transform;

            matBarra = Mat(Color.white);
            matPanel = Mat(new Color(0.14f, 0.17f, 0.24f));
            matBoton = Mat(new Color(0.18f, 0.22f, 0.30f));
            matGrilla = Mat(new Color(0.35f, 0.40f, 0.52f));
            matLinea = Mat(new Color(0.30f, 0.70f, 1f));
            matMapa = Mat(new Color(0.10f, 0.20f, 0.18f));

            Sala(raiz, Mat(new Color(0.10f, 0.12f, 0.17f)), Mat(new Color(0.07f, 0.09f, 0.13f)));
            Luz(raiz);
            var prefab = PrefabBarra(matBarra);

            // Contenedor de los 3 gráficos: en AR, ARPlacement mueve ESTE objeto al plano detectado.
            var cont = new GameObject("ContenedorGraficos").transform;
            cont.SetParent(raiz); cont.localPosition = new Vector3(0, 0.02f, 6f);

            var barras = GraficoBarras(cont, prefab);
            var lineas = GraficoLineas(cont, prefab);
            var mapa = GraficoMapa(cont, prefab);

            var panel = Panel(raiz);
            var detalle = PanelTexto(raiz, "Detalle", new Vector3(6.6f, 2.9f, 5.0f), 28f, new Vector3(4.2f, 2.0f, 0.1f), out _);
            var insight = PanelTexto(raiz, "Insight", new Vector3(6.9f, 1.1f, 3.9f), 28f, new Vector3(4.6f, 2.2f, 0.1f), out _);
            var (histTxt, histBotones) = PanelHistorial(raiz);
            panel.historial = histBotones;
            PanelControles(raiz);

            JugadorPC(raiz);
            RigVR(raiz);
            raiz.gameObject.AddComponent<FuenteMemoria>();
            var audio = raiz.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;

            var mgr = raiz.gameObject.AddComponent<DataRoomManager>();
            mgr.barras = barras; mgr.lineas = lineas; mgr.mapa = mapa;
            mgr.panel = panel; mgr.detalle = detalle; mgr.insightTxt = insight; mgr.historialTxt = histTxt;
            mgr.fuenteAudio = audio;

            Selection.activeTransform = raiz;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("DataRoom XR",
                "¡Sala armada!\n\nPulsa Play.\n\n• PC: haz clic en la sala para capturar el mouse, escribe tu pregunta y pulsa Enter. Esc libera el mouse.\n• O toca una pregunta sugerida.\n• Botones: TIPO (barras/líneas/mapa), EXPORTAR (CSV), LIMPIAR, MODO (VR/AR).\n• Selecciona una barra para ver su valor; dos barras para compararlas.\n\nLa base de datos ficticia de Junín se crea sola.\nPara IA real: Assets ▸ Create ▸ DataRoomXR ▸ Config IA y pon tu API key.",
                "Entendido");
            Debug.Log("[DataRoomXR] Sala armada con 3 gráficos. Pulsa Play.");
        }

        [MenuItem("Tools/DataRoom XR/Preparar AR (instrucciones)", false, 20)]
        public static void InstruccionesAR()
        {
            EditorUtility.DisplayDialog("DataRoom XR · AR (Android)",
                "AR usa paquetes GRATIS de Unity (no Asset Store):\n\n" +
                "1) Package Manager: instala AR Foundation + Google ARCore XR Plugin + XR Plugin Management.\n" +
                "2) Project Settings ▸ XR Plug-in Management ▸ Android: marca ARCore.\n" +
                "3) Player ▸ Scripting Define Symbols (Android): agrega  USAR_AR_FOUNDATION\n" +
                "4) Crea AR Session + XR Origin (AR) con AR Plane Manager y AR Raycast Manager.\n" +
                "5) Añade el script ARPlacement al XR Origin y arrastra 'ContenedorGraficos' a su campo 'contenedor'.\n" +
                "6) Build a Android: toca un plano para posar el dashboard.\n\n" +
                "Todo el detalle está en SETUP_AR_VR.md y en manifest-paquetes-AR-VR.json.",
                "Entendido");
        }

        [MenuItem("Tools/DataRoom XR/Preparar VR (instrucciones)", false, 21)]
        public static void InstruccionesVR()
        {
            EditorUtility.DisplayDialog("DataRoom XR · VR",
                "VR usa paquetes GRATIS de Unity:\n\n" +
                "• Cardboard (celular): instala el Google Cardboard XR Plugin, márcalo en XR Plug-in Management (Android),\n" +
                "  activa el objeto 'RigVR (desactivado)' y desactiva 'JugadorPC'.\n" +
                "• PC/OpenXR: instala OpenXR + XR Interaction Toolkit; activa RigVR o usa un XR Origin.\n\n" +
                "La selección en VR es POR MIRADA (apuntas con la cabeza ~1 s). Detalle en SETUP_AR_VR.md.",
                "Entendido");
        }

        // ========================= SALA =========================

        static void Sala(Transform raiz, Material pared, Material piso)
        {
            var g = new GameObject("Sala").transform; g.SetParent(raiz);
            var p = GameObject.CreatePrimitive(PrimitiveType.Plane);
            p.name = "Piso"; p.transform.SetParent(g); p.transform.localScale = new Vector3(4f, 1f, 4f);
            p.GetComponent<Renderer>().sharedMaterial = piso;
            Pared(g, pared, new Vector3(0, 3, 20), new Vector3(40, 6, 1));
            Pared(g, pared, new Vector3(0, 3, -20), new Vector3(40, 6, 1));
            Pared(g, pared, new Vector3(20, 3, 0), new Vector3(1, 6, 40));
            Pared(g, pared, new Vector3(-20, 3, 0), new Vector3(1, 6, 40));
            var techo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            techo.name = "Techo"; techo.transform.SetParent(g);
            techo.transform.localPosition = new Vector3(0, 6, 0);
            techo.transform.localScale = new Vector3(4f, 1f, 4f);
            techo.transform.localEulerAngles = new Vector3(180, 0, 0);
            techo.GetComponent<Renderer>().sharedMaterial = pared;
        }

        static void Pared(Transform p, Material m, Vector3 pos, Vector3 esc)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "Pared"; c.transform.SetParent(p);
            c.transform.localPosition = pos; c.transform.localScale = esc;
            c.GetComponent<Renderer>().sharedMaterial = m;
        }

        static void Luz(Transform raiz)
        {
            var g = new GameObject("Luz"); g.transform.SetParent(raiz);
            g.transform.localPosition = new Vector3(0, 5.5f, 4);
            var l = g.AddComponent<Light>();
            l.type = LightType.Point; l.range = 70; l.intensity = 1.4f; l.color = new Color(0.9f, 0.95f, 1f);
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.45f);
            RenderSettings.fog = false;
        }

        // ========================= GRÁFICOS =========================

        static GraficoBarras3D GraficoBarras(Transform raiz, BarraDato prefab)
        {
            var g = new GameObject("GraficoBarras").transform; g.SetParent(raiz);
            g.localPosition = Vector3.zero;   // relativo al ContenedorGraficos
            var c = g.gameObject.AddComponent<GraficoBarras3D>();
            c.prefabBarra = prefab; c.matGrilla = matGrilla;
            c.titulo = Texto(g, "Resultado", new Vector3(0, 5.2f, 0), 0.5f, TextAnchor.LowerCenter, Color.white, 70, true);
            c.ejeValor = Texto(g, "Valor", new Vector3(-7f, 2.5f, 0), 0.3f, TextAnchor.MiddleCenter, new Color(0.8f, 0.85f, 1f), 50, true);
            c.ejeCategoria = Texto(g, "Categoría", new Vector3(0, -0.6f, 0), 0.3f, TextAnchor.UpperCenter, new Color(0.8f, 0.85f, 1f), 50, true);
            c.piso = BasePiso(g, Mat(new Color(0.07f, 0.09f, 0.13f)));
            return c;
        }

        static GraficoLineas3D GraficoLineas(Transform raiz, BarraDato prefab)
        {
            var g = new GameObject("GraficoLineas").transform; g.SetParent(raiz);
            g.localPosition = Vector3.zero;   // relativo al ContenedorGraficos
            var c = g.gameObject.AddComponent<GraficoLineas3D>();
            c.prefabBarra = prefab; c.matLinea = matLinea;
            c.titulo = Texto(g, "Tendencia", new Vector3(0, 5.2f, 0), 0.5f, TextAnchor.LowerCenter, Color.white, 70, true);
            c.ejeValor = Texto(g, "Valor", new Vector3(-6f, 2.5f, 0), 0.3f, TextAnchor.MiddleCenter, new Color(0.8f, 0.85f, 1f), 50, true);
            c.ejeCategoria = Texto(g, "Año", new Vector3(0, -0.6f, 0), 0.3f, TextAnchor.UpperCenter, new Color(0.8f, 0.85f, 1f), 50, true);
            c.piso = BasePiso(g, Mat(new Color(0.07f, 0.09f, 0.13f)));
            g.gameObject.SetActive(false);
            return c;
        }

        static MapaJunin3D GraficoMapa(Transform raiz, BarraDato prefab)
        {
            var g = new GameObject("MapaJunin").transform; g.SetParent(raiz);
            g.localPosition = Vector3.zero;   // relativo al ContenedorGraficos
            var c = g.gameObject.AddComponent<MapaJunin3D>();
            c.prefabBarra = prefab;
            c.titulo = Texto(g, "Mapa de Junín", new Vector3(0, 5.2f, 0), 0.5f, TextAnchor.LowerCenter, Color.white, 70, true);
            var baseM = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseM.name = "BaseMapa"; baseM.transform.SetParent(g);
            Object.DestroyImmediate(baseM.GetComponent<BoxCollider>());
            baseM.transform.localScale = new Vector3(10f, 0.04f, 10f);
            baseM.transform.localPosition = new Vector3(0, -0.02f, 2.5f);
            baseM.GetComponent<Renderer>().sharedMaterial = matMapa;
            c.baseMapa = baseM.transform;
            g.gameObject.SetActive(false);
            return c;
        }

        static Transform BasePiso(Transform g, Material m)
        {
            var piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "BasePlataforma"; piso.transform.SetParent(g);
            Object.DestroyImmediate(piso.GetComponent<BoxCollider>());
            piso.transform.localScale = new Vector3(10f, 0.05f, 4f);
            piso.transform.localPosition = new Vector3(0, -0.03f, 0);
            piso.GetComponent<Renderer>().sharedMaterial = m;
            return piso.transform;
        }

        static BarraDato PrefabBarra(Material mat)
        {
            var root = new GameObject("BarraDato");
            var bd = root.AddComponent<BarraDato>();
            var col = GameObject.CreatePrimitive(PrimitiveType.Cube);
            col.name = "Columna"; col.transform.SetParent(root.transform);
            col.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            col.transform.localPosition = new Vector3(0, 0.5f, 0);
            var rend = col.GetComponent<Renderer>(); rend.sharedMaterial = mat;
            bd.columna = col.transform; bd.render = rend;
            bd.etiqueta = Texto(root.transform, "", new Vector3(0, -0.15f, 0), 0.18f, TextAnchor.UpperCenter, Color.white, 50, false);
            bd.valorTxt = Texto(root.transform, "", new Vector3(0, 1.2f, 0), 0.2f, TextAnchor.LowerCenter, new Color(1f, 0.95f, 0.7f), 50, false);
            root.AddComponent<MiraACamara>().soloEstos = new[] { bd.etiqueta.transform, bd.valorTxt.transform };
            root.SetActive(false);
            return bd;
        }

        // ========================= PANELES =========================

        static PanelConsulta Panel(Transform raiz)
        {
            var g = new GameObject("PanelConsulta").transform; g.SetParent(raiz);
            g.localPosition = new Vector3(-6.6f, 2.6f, 5.0f);
            g.localEulerAngles = new Vector3(0, -28f, 0);
            var comp = g.gameObject.AddComponent<PanelConsulta>();

            Fondo(g, matPanel, new Vector3(0, 0, 0.05f), new Vector3(5.2f, 5.6f, 0.1f));
            Texto(g, "DataRoom XR · Pregúntale a los datos de Junín", new Vector3(0, 2.5f, 0), 0.2f, TextAnchor.MiddleCenter, Color.white, 50, false);
            comp.textoPregunta = Texto(g, "", new Vector3(0, 2.05f, 0), 0.2f, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.6f), 50, false);
            comp.textoEstado = Texto(g, "", new Vector3(0, 1.7f, 0), 0.15f, TextAnchor.MiddleCenter, Color.white, 50, false);

            Texto(g, "Preguntas sugeridas:", new Vector3(-2.3f, 1.35f, 0), 0.14f, TextAnchor.UpperLeft, new Color(0.7f, 0.8f, 1f), 40, false);
            var sug = TextoASQLReglas.Sugeridas();
            comp.sugeridas = new Boton3D[sug.Length];
            for (int i = 0; i < sug.Length; i++)
                comp.sugeridas[i] = Boton(g, new Vector3(0, 1.0f - i * 0.4f, 0), 4.6f, sug[i], "sug" + i, matBoton);

            // Botones de control
            string[] ids = { "tipo", "exportar", "foto", "limpiar", "modo" };
            string[] lbl = { "◱ TIPO", "⇩ EXPORTAR", "FOTO", "✕ LIMPIAR", "VR/AR" };
            var ctrlMat = Mat(new Color(0.22f, 0.30f, 0.42f));
            for (int i = 0; i < ids.Length; i++)
                Boton(g, new Vector3(-1.96f + i * 0.98f, -1.7f, 0), 0.92f, lbl[i], ids[i], ctrlMat);

            comp.textoSQL = Texto(g, "", new Vector3(-2.3f, -2.15f, 0), 0.12f, TextAnchor.UpperLeft, new Color(0.6f, 0.85f, 0.7f), 40, false);
            return comp;
        }

        static (TextMesh, Boton3D[]) PanelHistorial(Transform raiz)
        {
            var g = new GameObject("PanelHistorial").transform; g.SetParent(raiz);
            g.localPosition = new Vector3(-10.2f, 2.6f, 1.2f);
            g.localEulerAngles = new Vector3(0, -52f, 0);
            Fondo(g, matPanel, new Vector3(0, 0, 0.05f), new Vector3(4.6f, 4.2f, 0.1f));
            Texto(g, "Historial", new Vector3(0, 1.8f, 0), 0.2f, TextAnchor.MiddleCenter, Color.white, 50, false);
            var txt = Texto(g, "", new Vector3(-2f, 1.45f, 0), 0.12f, TextAnchor.UpperLeft, new Color(0.75f, 0.82f, 0.95f), 40, false);
            var botones = new Boton3D[6];
            for (int i = 0; i < 6; i++)
            {
                botones[i] = Boton(g, new Vector3(0, 0.3f - i * 0.4f, 0), 4.1f, "", "hist" + i, matBoton);
                botones[i].Ocultar();
            }
            return (txt, botones);
        }

        static void PanelControles(Transform raiz)
        {
            var g = new GameObject("PanelControles").transform; g.SetParent(raiz);
            g.localPosition = new Vector3(10.2f, 2.6f, 1.2f);
            g.localEulerAngles = new Vector3(0, 52f, 0);
            Fondo(g, matPanel, new Vector3(0, 0, 0.05f), new Vector3(4.6f, 3.2f, 0.1f));
            Texto(g, "Controles", new Vector3(0, 1.3f, 0), 0.2f, TextAnchor.MiddleCenter, Color.white, 50, false);
            string cuerpo =
                "WASD: mover\n" +
                "Mouse: mirar\n" +
                "Clic / E: seleccionar\n" +
                "Selecciona 2 barras: comparar\n" +
                "TIPO: barras/líneas/mapa\n" +
                "EXPORTAR: CSV   LIMPIAR   MODO: VR/AR\n" +
                "Tab: panel   Esc: liberar mouse\n" +
                "F9: capturar 3 vistas";
            Texto(g, cuerpo, new Vector3(-2.1f, 0.95f, 0), 0.14f, TextAnchor.UpperLeft, new Color(0.78f, 0.85f, 0.95f), 40, false);
        }

        static TextMesh PanelTexto(Transform raiz, string titulo, Vector3 pos, float rotY, Vector3 tam, out Transform g)
        {
            var go = new GameObject("Panel" + titulo).transform; go.SetParent(raiz);
            go.localPosition = pos; go.localEulerAngles = new Vector3(0, rotY, 0); g = go;
            Fondo(go, matPanel, new Vector3(0, 0, 0.05f), tam);
            Texto(go, titulo, new Vector3(0, tam.y / 2f - 0.3f, 0), 0.2f, TextAnchor.MiddleCenter, new Color(0.8f, 0.85f, 1f), 50, false);
            return Texto(go, "", new Vector3(0, 0, 0), 0.18f, TextAnchor.MiddleCenter, Color.white, 45, false);
        }

        static Boton3D Boton(Transform p, Vector3 pos, float ancho, string texto, string id, Material mat)
        {
            var g = new GameObject("Boton_" + id);
            g.transform.SetParent(p); g.transform.localPosition = pos;
            var b = g.AddComponent<Boton3D>();
            var fondo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fondo.name = "Fondo"; fondo.transform.SetParent(g.transform);
            fondo.transform.localScale = new Vector3(ancho, 0.34f, 0.05f);
            fondo.GetComponent<Renderer>().sharedMaterial = mat;
            b.fondo = fondo.GetComponent<Renderer>();
            b.etiqueta = Texto(g.transform, texto, new Vector3(0, 0, -0.06f), 0.12f, TextAnchor.MiddleCenter, Color.white, 40, false);
            b.Configurar(id, texto);
            return b;
        }

        // ========================= JUGADORES =========================

        static void JugadorPC(Transform raiz)
        {
            var g = new GameObject("JugadorPC"); g.transform.SetParent(raiz);
            g.transform.localPosition = new Vector3(0, 0, 0);
            var cc = g.AddComponent<CharacterController>();
            cc.height = 1.7f; cc.center = new Vector3(0, 0.85f, 0); cc.radius = 0.3f;
            var cabeza = new GameObject("Cabeza").transform;
            cabeza.SetParent(g.transform); cabeza.localPosition = new Vector3(0, 1.6f, 0);
            var camGo = new GameObject("Camara");
            camGo.transform.SetParent(cabeza);
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
            cam.gameObject.AddComponent<ManoPuntero>();
            var fps = g.AddComponent<JugadorFPS>();
            fps.cabeza = cabeza; fps.camara = cam;
            g.AddComponent<CamaraOrbital>();
        }

        static void RigVR(Transform raiz)
        {
            var g = new GameObject("RigVR (desactivado)"); g.transform.SetParent(raiz);
            g.transform.localPosition = new Vector3(0, 0, 0);
            var camGo = new GameObject("CamaraVR");
            camGo.transform.SetParent(g.transform);
            camGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            camGo.AddComponent<Camera>();
            camGo.AddComponent<ManoPuntero>();
            var vr = g.AddComponent<JugadorVR>();
            vr.camaraVR = camGo.GetComponent<Camera>();
            g.SetActive(false);
        }

        // ========================= UTILIDADES =========================

        static void Fondo(Transform p, Material m, Vector3 pos, Vector3 esc)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "Fondo"; c.transform.SetParent(p);
            c.transform.localPosition = pos; c.transform.localScale = esc;
            Object.DestroyImmediate(c.GetComponent<BoxCollider>());
            c.GetComponent<Renderer>().sharedMaterial = m;
        }

        static TextMesh Texto(Transform p, string s, Vector3 pos, float size, TextAnchor anchor, Color color, int fontSize, bool billboard)
        {
            var g = new GameObject("Texto");
            g.transform.SetParent(p); g.transform.localPosition = pos;
            g.transform.localScale = Vector3.one * size;
            var tm = g.AddComponent<TextMesh>();
            tm.text = s; tm.font = Fuente; tm.fontSize = fontSize; tm.characterSize = 0.1f;
            tm.anchor = anchor; tm.alignment = TextAlignment.Center; tm.color = color;
            g.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            g.AddComponent<Texto3D>();
            if (billboard) g.AddComponent<MiraACamara>();
            return tm;
        }

        static Material Mat(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(sh); m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }
    }
}
#endif
