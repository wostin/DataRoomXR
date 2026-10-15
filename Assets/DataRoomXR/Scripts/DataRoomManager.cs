using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DataRoomXR
{
    public enum Modo { VR, AR }

    /// <summary>
    /// Cerebro de DataRoom XR. Flujo:
    ///   pregunta (texto/voz/botón) -> TextoASQL (IA o reglas) -> SeguridadSQL -> fuente.Consultar
    ///   -> elige gráfico (barras/líneas/mapa) -> dibuja -> insight automático + detalle al seleccionar.
    /// Guarda cada consulta en metricas.csv y permite exportar el último resultado a CSV.
    /// </summary>
    public class DataRoomManager : MonoBehaviour
    {
        public static DataRoomManager I { get; private set; }

        [Header("Gráficos (los asigna el armador de escena)")]
        public GraficoBarras3D barras;
        public GraficoLineas3D lineas;
        public MapaJunin3D mapa;

        [Header("Paneles")]
        public PanelConsulta panel;
        public TextMesh detalle;         // detalle al seleccionar una barra / comparación
        public TextMesh insightTxt;      // insight automático del resultado
        public TextMesh historialTxt;    // lista de preguntas recientes
        public AudioSource fuenteAudio;

        [Header("Opciones")]
        public Modo modo = Modo.VR;
        public bool registrarMetricas = true;
        [Tooltip("Consulta que se muestra sola al iniciar (deja vacío para arrancar sin gráfico).")]
        public string preguntaInicial = "Población por distrito";
        [Tooltip("Modo demostración: va mostrando solo las 3 visualizaciones hasta que el usuario interactúe.")]
        public bool autoDemo = true;
        public float segundosDemo = 9f;

        // Preguntas que recorre el modo demo (barras, líneas y mapa).
        static readonly string[] DemoPreguntas = {
            "Población por distrito",
            "Evolución de la producción de papa en Junín por año",
            "Mapa de cobertura de internet por distrito en 2024",
            "¿Qué distrito tiene más matrícula escolar en 2025?",
            "¿Qué distrito recicla menos residuos en 2025?",
        };

        IFuenteDatos fuente;
        IGrafico3D graficoActivo;
        string pregunta = "";
        bool ocupado;
        float tInicioConsulta;
        AudioClip sListo, sError, sClic;
        string rutaCsv;

        Resultado ultimoResultado;
        PlanConsulta ultimoPlan;
        readonly List<string> historial = new List<string>();
        BarraDato compararA;   // para comparar dos barras
        public bool mostrandoMenu = true;

        void Awake()
        {
            I = this;
            mostrandoMenu = true;
            fuente = GetComponentInChildren(typeof(IFuenteDatos)) as IFuenteDatos;
            if (fuente == null)
            {
#pragma warning disable 618
#if UNITY_2023_1_OR_NEWER
                var todos = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
#else
                var todos = FindObjectsOfType<MonoBehaviour>();
#endif
#pragma warning restore 618
                foreach (var mb in todos) if (mb is IFuenteDatos f) { fuente = f; break; }
            }
            sListo = Sonidos.Listo();
            sError = Sonidos.Error();
            sClic = Sonidos.Clic();
            rutaCsv = Path.Combine(Application.persistentDataPath, "metricas.csv");
            if (registrarMetricas && !File.Exists(rutaCsv))
                File.WriteAllText(rutaCsv, "fecha,modo,fuente,pregunta,sql,valido,filas,segundos\n", Encoding.UTF8);

            var audioAmbiente = gameObject.AddComponent<AudioSource>();
            audioAmbiente.clip = Sonidos.Ambiente();
            audioAmbiente.loop = true;
            audioAmbiente.volume = 0.15f;
            audioAmbiente.playOnAwake = false;
            audioAmbiente.Play();
        }

        void Start()
        {
            if (panel != null)
            {
                panel.CargarSugeridas(TextoASQLReglas.Sugeridas());
                panel.MostrarPregunta("");
                panel.MostrarEstado("Escribe una pregunta o toca una sugerida", Color.white);
                panel.MostrarSQL("");
            }
            if (detalle != null) detalle.text = "Selecciona una barra para ver su valor.\nSelecciona dos para compararlas.";
            if (insightTxt != null) insightTxt.text = "";
            ActualizarHistorial();
            OcultarGraficos();

            if (!mostrandoMenu) Arrancar();
        }

        public void Arrancar()
        {
            if (fuente == null)
            {
                fuente = GetComponentInChildren(typeof(IFuenteDatos)) as IFuenteDatos;
                if (fuente == null)
                {
                    foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                        if (mb is IFuenteDatos f) { fuente = f; break; }
                }
            }

            // Consulta de bienvenida: muestra un gráfico apenas inicia, en vez de la sala vacía.
            if (!string.IsNullOrEmpty(preguntaInicial) && fuente != null)
                Preguntar(preguntaInicial);

            if (autoDemo && fuente != null) StartCoroutine(RutinaDemo());
        }

        // Modo demostración: recorre las preguntas de ejemplo hasta que el usuario interactúe.
        IEnumerator RutinaDemo()
        {
            int i = 0;
            while (autoDemo)
            {
                yield return new WaitForSeconds(segundosDemo);
                if (!autoDemo) yield break;
                if (ocupado) continue;
                i = (i + 1) % DemoPreguntas.Length;
                Preguntar(DemoPreguntas[i]);
            }
        }

        // El usuario tomó el control: se apaga la demo automática.
        void DetenerDemo()
        {
            if (!autoDemo) return;
            autoDemo = false;
            if (panel != null) panel.MostrarEstado("Modo demo detenido. Escribe tu propia pregunta.", Color.white);
        }

        void Update()
        {
            if (mostrandoMenu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            bool f9 = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame) f9 = true;
#else
            if (Input.GetKeyDown(KeyCode.F9)) f9 = true;
#endif
            if (f9) StartCoroutine(CapturarTresVistas());

            if (!EstadoJugador.ModoVR && !ocupado) LeerTeclado();
        }

        GUIStyle estiloTitulo, estiloSub, estiloAutor, estiloBoton, estiloCaja, estiloNota;

        void OnGUI()
        {
            if (!mostrandoMenu)
            {
                DibujarNotaHonestidad();
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Color cAnt = GUI.color;
            GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = cAnt;

            float ancho = 560f, alto = 320f;
            float x = (Screen.width - ancho) / 2f;
            float y = (Screen.height - alto) / 2f;

            if (estiloCaja == null)
            {
                var texCaja = new Texture2D(1, 1);
                texCaja.SetPixel(0, 0, new Color(0.10f, 0.14f, 0.22f, 0.96f));
                texCaja.Apply();

                estiloCaja = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = texCaja }
                };

                estiloTitulo = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 36,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.96f, 1f) }
                };

                estiloSub = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.35f, 0.75f, 1f) }
                };

                estiloAutor = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.75f, 0.82f, 0.92f) }
                };

                var texBtn = new Texture2D(1, 1);
                texBtn.SetPixel(0, 0, new Color(0.18f, 0.45f, 0.85f));
                texBtn.Apply();

                var texBtnHover = new Texture2D(1, 1);
                texBtnHover.SetPixel(0, 0, new Color(0.25f, 0.58f, 0.95f));
                texBtnHover.Apply();

                estiloBoton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { background = texBtn, textColor = Color.white },
                    hover = { background = texBtnHover, textColor = Color.white }
                };
            }

            GUI.Box(new Rect(x, y, ancho, alto), GUIContent.none, estiloCaja);

            GUI.Label(new Rect(x + 20, y + 35, ancho - 40, 48), "DataRoom XR", estiloTitulo);
            GUI.Label(new Rect(x + 20, y + 90, ancho - 40, 28), "Inteligencia de negocios inmersiva · Junín", estiloSub);
            GUI.Label(new Rect(x + 20, y + 140, ancho - 40, 24), "Desarrollado por: Jumpa Fierro, Jhovani Salvador", estiloAutor);

            float btnAncho = 220f, btnAlto = 50f;
            if (GUI.Button(new Rect(x + (ancho - btnAncho) / 2f, y + 215, btnAncho, btnAlto), "INICIAR", estiloBoton))
            {
                IniciarDesdeMenu();
            }
        }

        public void IniciarDesdeMenu()
        {
            mostrandoMenu = false;
            Sonar(sClic);
            Arrancar();
        }

        void DibujarNotaHonestidad()
        {
            if (estiloNota == null)
            {
                estiloNota = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.LowerLeft,
                    normal = { textColor = new Color(0.78f, 0.86f, 0.98f, 0.65f) }
                };
            }
            GUI.Label(new Rect(16, Screen.height - 28, 320, 20), "Datos ficticios · demo académica", estiloNota);
        }

        // ---------------- Entrada de texto (PC) ----------------

        void LeerTeclado()
        {
            if (Entrada.Enter()) { DetenerDemo(); Preguntar(pregunta); return; }
            bool cambio = false;
            if (Entrada.Backspace() && pregunta.Length > 0) { pregunta = pregunta.Substring(0, pregunta.Length - 1); cambio = true; }
            foreach (char c in Entrada.TextoEscrito())
            {
                if (c == '\b') { if (pregunta.Length > 0) pregunta = pregunta.Substring(0, pregunta.Length - 1); cambio = true; }
                else if (c == '\n' || c == '\r') { DetenerDemo(); Preguntar(pregunta); return; }
                else if (!char.IsControl(c)) { pregunta += c; cambio = true; }
            }
            if (cambio) { DetenerDemo(); if (panel != null) panel.MostrarPregunta(pregunta); }
        }

        // ---------------- Botones 3D ----------------

        public void PulsarBoton(Boton3D b)
        {
            Sonar(sClic);
            if (b.id == null) return;
            if (b.id.StartsWith("sug")) { DetenerDemo(); Preguntar(b.Texto); }
            else if (b.id.StartsWith("hist")) { DetenerDemo(); Preguntar(b.Texto); }
            else if (b.id == "modo") CambiarModo();
            else if (b.id == "limpiar") LimpiarTodo();
            else if (b.id == "exportar") ExportarResultado();
            else if (b.id == "tipo") CambiarTipoGrafico();
            else if (b.id == "foto") Capturar();
        }

        public void CambiarModo()
        {
            modo = modo == Modo.VR ? Modo.AR : Modo.VR;
            if (panel != null) panel.MostrarEstado("Modo de visualización: " + modo, Color.white);
        }

        void LimpiarTodo()
        {
            OcultarGraficos();
            if (detalle != null) detalle.text = "";
            if (insightTxt != null) insightTxt.text = "";
            compararA = null;
        }

        // Permite forzar el tipo de gráfico sobre el mismo resultado (barras -> líneas -> mapa).
        void CambiarTipoGrafico()
        {
            if (ultimoResultado == null || !ultimoResultado.Ok) return;
            string[] ciclo = { "barras", "lineas", "mapa" };
            int idx = Array.IndexOf(ciclo, ultimoPlan != null ? ultimoPlan.grafico : "barras");
            for (int k = 1; k <= ciclo.Length; k++)
            {
                string sig = ciclo[(idx + k) % ciclo.Length];
                if (ultimoPlan != null) ultimoPlan.grafico = sig;
                if (MostrarResultado(ultimoResultado, ultimoPlan)) { if (panel != null) panel.MostrarEstado("Gráfico: " + sig, Color.white); return; }
            }
        }

        // ---------------- Consulta ----------------

        public void Preguntar(string texto)
        {
            if (ocupado || string.IsNullOrWhiteSpace(texto)) return;
            pregunta = texto.Trim();
            if (panel != null) { panel.MostrarPregunta(pregunta); panel.MostrarEstado("Pensando...", new Color(1f, 0.8f, 0.2f)); panel.MostrarSQL(""); }
            StartCoroutine(Flujo(pregunta));
        }

        IEnumerator Flujo(string texto)
        {
            ocupado = true;
            tInicioConsulta = Time.realtimeSinceStartup;
            string esquema = fuente != null ? fuente.Esquema : "";
            PlanConsulta plan = null; string errorIA = null;

            yield return TextoASQL.Traducir(texto, esquema, p => plan = p, e => errorIA = e);

            if (plan == null)
            {
                Fallar(errorIA ?? "No se pudo generar la consulta.");
                Registrar(texto, "", false, 0); ocupado = false; yield break;
            }

            if (panel != null) panel.MostrarSQL(plan.sql);

            string errVal = SeguridadSQL.Validar(plan.sql);
            if (errVal != null) { Fallar("Consulta rechazada: " + errVal); Registrar(texto, plan.sql, false, 0); ocupado = false; yield break; }

            Resultado r = fuente != null ? fuente.Consultar(plan.sql) : new Resultado { error = "Sin fuente de datos en la escena." };
            if (!r.Ok) { Fallar(r.error); Registrar(texto, plan.sql, false, 0); ocupado = false; yield break; }

            ultimoResultado = r; ultimoPlan = plan;
            MostrarResultado(r, plan);
            MostrarInsight(r, plan);
            compararA = null;

            float seg = Time.realtimeSinceStartup - tInicioConsulta;
            string origen = ConfigIA.Cargar().Disponible ? "Consulta por IA" : "Consulta por reglas (sin IA)";
            if (panel != null) panel.MostrarEstado(origen + " · " + r.filas.Count + " filas · " + seg.ToString("0.0") + " s", new Color(0.3f, 0.85f, 0.4f));

            AgregarHistorial(texto);
            Sonar(sListo);
            Registrar(texto, plan.sql, true, r.filas.Count);
            pregunta = "";
            ocupado = false;
        }

        // Elige el gráfico según el plan y con respaldos sensatos.
        bool MostrarResultado(Resultado r, PlanConsulta plan)
        {
            OcultarGraficos();
            string tipo = plan != null && !string.IsNullOrEmpty(plan.grafico) ? plan.grafico.ToLowerInvariant() : "barras";

            // Orden de intento según el tipo pedido, con respaldo a barras.
            IGrafico3D[] intento;
            if (tipo == "mapa") intento = new IGrafico3D[] { mapa, barras, lineas };
            else if (tipo == "lineas") intento = new IGrafico3D[] { lineas, barras, mapa };
            else intento = new IGrafico3D[] { barras, lineas, mapa };

            foreach (var g in intento)
            {
                if (g == null) continue;
                g.Mostrar(true);
                if (g.Dibujar(r, plan)) { graficoActivo = g; return true; }
                g.Mostrar(false);
            }
            return false;
        }

        void OcultarGraficos()
        {
            if (barras != null) { barras.Limpiar(); barras.Mostrar(false); }
            if (lineas != null) { lineas.Limpiar(); lineas.Mostrar(false); }
            if (mapa != null) { mapa.Limpiar(); mapa.Mostrar(false); }
            graficoActivo = null;
        }

        void Fallar(string msg)
        {
            if (panel != null) panel.MostrarEstado(msg, new Color(0.95f, 0.4f, 0.35f));
            Sonar(sError);
        }

        // ---------------- Insight automático (local) ----------------

        void MostrarInsight(Resultado r, PlanConsulta plan)
        {
            if (insightTxt == null) return;
            var ins = Insight.De(r);
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(plan != null ? plan.explicacion : null)) sb.Append(plan.explicacion).Append('\n');
            if (ins.esSerieTemporal)
            {
                string signo = ins.variacionPct >= 0 ? "+" : "";
                sb.Append("Tendencia: ").Append(signo).Append(ins.variacionPct.ToString("0.0")).Append("% del primer al último año.\n");
                sb.Append("Promedio: ").Append(GraficoBarras3D.Formato(ins.promedio)).Append(" · Total: ").Append(GraficoBarras3D.Formato(ins.total));
            }
            else
            {
                sb.Append("Lidera: <b>").Append(ins.lider).Append("</b> (").Append(GraficoBarras3D.Formato(ins.liderValor)).Append(")\n");
                sb.Append("Menor: ").Append(ins.menor).Append(" (").Append(GraficoBarras3D.Formato(ins.menorValor)).Append(")\n");
                sb.Append("Promedio: ").Append(GraficoBarras3D.Formato(ins.promedio));
                if (ins.promedio > 0 && ins.lider != null)
                    sb.Append(" · el líder es ").Append((ins.liderValor / ins.promedio).ToString("0.0")).Append("x el promedio");
            }
            insightTxt.text = sb.ToString();
        }

        // ---------------- Selección y comparación ----------------

        public void SeleccionarBarra(BarraDato b)
        {
            if (b == null || detalle == null) return;
            EmitirDestello(b.columna != null ? b.columna.position : b.transform.position);

            if (compararA == null)
            {
                compararA = b;
                detalle.text = "<b>" + b.Etiqueta + "</b>: " + GraficoBarras3D.Formato(b.Valor) +
                               "\n\nSelecciona otra barra para comparar.";
                return;
            }
            if (compararA == b)
            {
                detalle.text = "<b>" + b.Etiqueta + "</b>: " + GraficoBarras3D.Formato(b.Valor);
                compararA = null;
                return;
            }

            // comparación A vs B
            double a = compararA.Valor, c = b.Valor;
            double dif = c - a;
            double pct = a != 0 ? dif / a * 100.0 : 0;
            string signo = dif >= 0 ? "+" : "";
            var sb = new StringBuilder();
            sb.Append("<b>Comparación</b>\n");
            sb.Append(compararA.Etiqueta).Append(": ").Append(GraficoBarras3D.Formato(a)).Append('\n');
            sb.Append(b.Etiqueta).Append(": ").Append(GraficoBarras3D.Formato(c)).Append('\n');
            sb.Append("Diferencia: ").Append(signo).Append(GraficoBarras3D.Formato(dif));
            sb.Append(" (").Append(signo).Append(pct.ToString("0.0")).Append("%)");
            detalle.text = sb.ToString();
            compararA = null;
        }

        void EmitirDestello(Vector3 pos)
        {
            var go = new GameObject("DestelloBarra");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var ren = go.GetComponent<ParticleSystemRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            if (sh != null) ren.sharedMaterial = new Material(sh);

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.6f;
            main.startSpeed = 2f;
            main.startSize = 0.12f;
            main.startColor = new Color(0.9f, 0.96f, 1f, 0.95f);
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            ps.Play();
            Destroy(go, 2f);
        }

        // ---------------- Historial ----------------

        void AgregarHistorial(string preg)
        {
            historial.Remove(preg);
            historial.Insert(0, preg);
            while (historial.Count > 6) historial.RemoveAt(historial.Count - 1);
            ActualizarHistorial();
            if (panel != null) panel.CargarHistorial(historial.ToArray());
        }

        void ActualizarHistorial()
        {
            if (historialTxt == null) return;
            if (historial.Count == 0) { historialTxt.text = "Tus preguntas recientes aparecerán aquí."; return; }
            var sb = new StringBuilder("<b>Recientes</b>\n");
            foreach (var h in historial) sb.Append("• ").Append(h).Append('\n');
            historialTxt.text = sb.ToString();
        }

        // ---------------- Exportar resultado ----------------

        void ExportarResultado()
        {
            if (ultimoResultado == null || !ultimoResultado.Ok)
            {
                if (panel != null) panel.MostrarEstado("No hay resultado para exportar.", new Color(0.95f, 0.6f, 0.3f));
                return;
            }
            try
            {
                string nombre = "resultado_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                string ruta = Path.Combine(Application.persistentDataPath, nombre);
                var sb = new StringBuilder();
                sb.Append(Csv(ultimoResultado.tituloEtiqueta)).Append(',').Append(Csv(ultimoResultado.tituloValor)).Append('\n');
                foreach (var f in ultimoResultado.filas)
                    sb.Append(Csv(f.etiqueta)).Append(',').Append(f.valor.ToString(CultureInfo.InvariantCulture)).Append('\n');
                File.WriteAllText(ruta, sb.ToString(), Encoding.UTF8);
                if (panel != null) panel.MostrarEstado("Exportado: " + nombre, new Color(0.3f, 0.85f, 0.4f));
                Debug.Log("[DataRoomXR] Resultado exportado a: " + ruta);
            }
            catch (Exception e)
            {
                if (panel != null) panel.MostrarEstado("Error al exportar: " + e.Message, new Color(0.95f, 0.4f, 0.35f));
            }
        }

        // ---------------- Captura de pantalla ----------------

        void Capturar()
        {
            try
            {
                string fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string nombre = "dataroom_" + fecha + ".png";
                string ruta = Path.Combine(Application.persistentDataPath, nombre);
                ScreenCapture.CaptureScreenshot(ruta);
                if (panel != null) panel.MostrarEstado("Foto guardada: " + ruta, new Color(0.3f, 0.85f, 0.4f));
                Debug.Log("[DataRoomXR] Captura guardada en: " + ruta);
            }
            catch (Exception e)
            {
                if (panel != null) panel.MostrarEstado("Error al capturar: " + e.Message, new Color(0.95f, 0.4f, 0.35f));
            }
        }

        public IEnumerator CapturarTresVistas()
        {
            autoDemo = false;
            DetenerDemo();

            var vistas = new (string pregunta, string nombre)[]
            {
                ("Población por distrito", "barras"),
                ("Evolución de la producción de papa en Junín por año", "lineas"),
                ("Mapa de cobertura de internet por distrito en 2024", "mapa")
            };

            foreach (var (pregunta, nombre) in vistas)
            {
                yield return new WaitUntil(() => !ocupado);
                Preguntar(pregunta);
                yield return new WaitUntil(() => !ocupado);
                yield return new WaitForSeconds(1.5f);

                string ruta = Path.Combine(Application.persistentDataPath, "vista_" + nombre + ".png");
                ScreenCapture.CaptureScreenshot(ruta);
                yield return new WaitForSeconds(0.5f);
            }

            if (panel != null)
                panel.MostrarEstado("3 capturas guardadas en " + Application.persistentDataPath, new Color(0.3f, 0.85f, 0.4f));
        }

        // ---------------- Métricas ----------------

        void Registrar(string preg, string sql, bool valido, int filas)
        {
            if (!registrarMetricas) return;
            float seg = Time.realtimeSinceStartup - tInicioConsulta;
            string fuenteSQL = ConfigIA.Cargar().Disponible ? "IA" : "reglas";
            string linea = string.Join(",",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), modo.ToString(), fuenteSQL,
                Csv(preg), Csv(sql), valido ? "1" : "0", filas.ToString(), seg.ToString("0.00", CultureInfo.InvariantCulture));
            try { File.AppendAllText(rutaCsv, linea + "\n", Encoding.UTF8); }
            catch (Exception e) { Debug.LogWarning("No se pudo escribir métricas: " + e.Message); }
        }

        static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            return "\"" + s.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ") + "\"";
        }

        void Sonar(AudioClip c) { if (fuenteAudio != null && c != null) fuenteAudio.PlayOneShot(c); }
    }
}
