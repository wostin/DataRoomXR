using System.Collections.Generic;
using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Gráfico de barras 3D. Instancia una BarraDato por fila, normaliza las alturas,
    /// reparte las barras en fila (o en rejilla si son muchas), colorea con una escala,
    /// dibuja líneas de referencia con etiquetas de valor en el eje vertical y escribe los títulos.
    /// Sirve igual en AR (sobre la mesa) y en VR (sala de datos): es geometría 3D normal.
    /// </summary>
    public class GraficoBarras3D : MonoBehaviour, IGrafico3D
    {
        [Header("Prefab de barra (lo arma el editor)")]
        public BarraDato prefabBarra;

        [Header("Layout")]
        public float separacion = 0.9f;
        public float alturaMaxima = 4f;
        public int columnasPorFila = 12;

        [Header("Rótulos")]
        public TextMesh titulo;
        public TextMesh ejeCategoria;
        public TextMesh ejeValor;
        public Transform piso;

        [Header("Eje de valor (grilla)")]
        public Material matGrilla;
        public int divisionesGrilla = 4;

        [Header("Colores (escala de menor a mayor)")]
        public Color colorBajo = new Color(0.20f, 0.42f, 0.78f);
        public Color colorAlto = new Color(0.95f, 0.55f, 0.15f);

        readonly List<BarraDato> barras = new List<BarraDato>();
        readonly List<GameObject> grilla = new List<GameObject>();
        float anchoTotal;

        public bool Dibujar(Resultado r, PlanConsulta plan)
        {
            Limpiar();
            if (r == null || r.filas.Count == 0) return false;

            int idxLider = 0;
            double max = double.MinValue, min = double.MaxValue;
            for (int i = 0; i < r.filas.Count; i++)
            {
                var f = r.filas[i];
                if (f.valor > max) { max = f.valor; idxLider = i; }
                if (f.valor < min) min = f.valor;
            }
            if (max <= 0) max = 1;
            double rango = (max - min) < 1e-9 ? max : (max - min);

            int n = r.filas.Count;
            int cols = Mathf.Min(columnasPorFila, n);
            int filasGrid = Mathf.CeilToInt(n / (float)cols);
            anchoTotal = (cols - 1) * separacion;
            float fondoTotal = (filasGrid - 1) * separacion;

            for (int i = 0; i < n; i++)
            {
                var fila = r.filas[i];
                int cx = i % cols, cz = i / cols;
                var barra = Instantiate(prefabBarra, transform);
                barra.gameObject.SetActive(true);
                barra.transform.localPosition = new Vector3(cx * separacion - anchoTotal / 2f, 0f, -(cz * separacion));
                float altura = (float)(fila.valor / max) * alturaMaxima;
                float t = (float)((fila.valor - min) / rango);
                barra.Configurar(fila.etiqueta, fila.valor, altura, Color.Lerp(colorBajo, colorAlto, t), Formato(fila.valor));
                barras.Add(barra);
            }

            DibujarGrilla(max);

            if (titulo != null) titulo.text = plan != null && !string.IsNullOrEmpty(plan.titulo) ? plan.titulo : "Resultado";
            if (ejeCategoria != null) ejeCategoria.text = plan != null && !string.IsNullOrEmpty(plan.eje_categoria) ? plan.eje_categoria : r.tituloEtiqueta;
            if (ejeValor != null) ejeValor.text = plan != null && !string.IsNullOrEmpty(plan.eje_valor) ? plan.eje_valor : r.tituloValor;

            if (piso != null)
            {
                piso.localScale = new Vector3((anchoTotal + separacion * 1.5f) / 10f, 1f, (fondoTotal + separacion * 1.5f) / 10f);
                piso.localPosition = new Vector3(0f, -0.01f, -fondoTotal / 2f);
            }

            // Leyenda de color (menor -> mayor) a la derecha
            float xLey = anchoTotal / 2f + 1.5f;
            float yBaseLey = 1.0f;
            float sepCubo = 0.35f;
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cubo.GetComponent<Collider>());
                cubo.name = "LeyendaCubo";
                cubo.transform.SetParent(transform);
                cubo.transform.localPosition = new Vector3(xLey, yBaseLey + i * sepCubo, 0f);
                cubo.transform.localScale = Vector3.one * 0.25f;
                var ren = cubo.GetComponent<Renderer>();
                Color c = Color.Lerp(colorBajo, colorAlto, t);
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                ren.SetPropertyBlock(mpb);
                ren.material.color = c;
                grilla.Add(cubo);
            }
            var txtMenor = NuevoTexto("Menor", new Vector3(xLey, yBaseLey - 0.3f, 0f), TextAnchor.MiddleCenter);
            grilla.Add(txtMenor.gameObject);
            var txtMayor = NuevoTexto("Mayor", new Vector3(xLey, yBaseLey + 4 * sepCubo + 0.3f, 0f), TextAnchor.MiddleCenter);
            grilla.Add(txtMayor.gameObject);

            // Placa "#1" dorada sobre la barra líder
            if (idxLider >= 0 && idxLider < n)
            {
                int cxL = idxLider % cols, czL = idxLider / cols;
                float xL = cxL * separacion - anchoTotal / 2f;
                float zL = -(czL * separacion);
                float yL = (float)(r.filas[idxLider].valor / max) * alturaMaxima + 0.5f;

                var placa = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(placa.GetComponent<Collider>());
                placa.name = "PlacaLider";
                placa.transform.SetParent(transform);
                placa.transform.localPosition = new Vector3(xL, yL, zL);
                placa.transform.localScale = new Vector3(0.55f, 0.32f, 0.1f);
                var renP = placa.GetComponent<Renderer>();
                Color dorado = new Color(1f, 0.82f, 0.18f);
                var mpbP = new MaterialPropertyBlock();
                mpbP.SetColor("_BaseColor", dorado);
                mpbP.SetColor("_Color", dorado);
                renP.SetPropertyBlock(mpbP);
                renP.material.color = dorado;
                grilla.Add(placa);

                var txtLider = NuevoTexto("#1", new Vector3(xL, yL, zL - 0.07f), TextAnchor.MiddleCenter);
                txtLider.color = new Color(0.12f, 0.09f, 0.02f);
                grilla.Add(txtLider.gameObject);
            }

            return true;
        }

        void DibujarGrilla(double max)
        {
            if (divisionesGrilla < 1) return;
            float x0 = -anchoTotal / 2f - separacion * 0.7f;
            float ancho = anchoTotal + separacion * 1.4f;
            for (int i = 0; i <= divisionesGrilla; i++)
            {
                float f = i / (float)divisionesGrilla;
                float y = f * alturaMaxima;
                // línea horizontal (cubo muy fino)
                var linea = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(linea.GetComponent<Collider>());
                linea.name = "Grilla";
                linea.transform.SetParent(transform);
                linea.transform.localPosition = new Vector3(0f, y, 0.3f);
                linea.transform.localScale = new Vector3(ancho, 0.01f, 0.01f);
                if (matGrilla != null) linea.GetComponent<Renderer>().sharedMaterial = matGrilla;
                grilla.Add(linea);
                // etiqueta de valor
                var et = NuevoTexto(Formato(max * f), new Vector3(x0 - 0.1f, y, 0.3f), TextAnchor.MiddleRight);
                grilla.Add(et.gameObject);
            }
        }

        TextMesh NuevoTexto(string s, Vector3 pos, TextAnchor anchor)
        {
            var g = new GameObject("ValorEje");
            g.transform.SetParent(transform);
            g.transform.localPosition = pos;
            g.transform.localScale = Vector3.one * 0.22f;
            var tm = g.AddComponent<TextMesh>();
            tm.text = s; tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 48; tm.characterSize = 0.1f; tm.anchor = anchor; tm.alignment = TextAlignment.Right;
            tm.color = new Color(0.7f, 0.78f, 0.9f);
            g.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            g.AddComponent<Texto3D>();
            g.AddComponent<MiraACamara>();
            return tm;
        }

        public void Limpiar()
        {
            foreach (var b in barras) if (b != null) Destroy(b.gameObject);
            barras.Clear();
            foreach (var g in grilla) if (g != null) Destroy(g);
            grilla.Clear();
        }

        public void Mostrar(bool v) { gameObject.SetActive(v); }

        public static string Formato(double v)
        {
            double a = System.Math.Abs(v);
            if (a >= 1_000_000) return (v / 1_000_000d).ToString("0.0") + " M";
            if (a >= 1_000) return (v / 1_000d).ToString("0.0") + " k";
            if (a == System.Math.Floor(a)) return v.ToString("0");
            return v.ToString("0.0");
        }
    }
}
