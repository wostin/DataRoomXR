using System.Collections.Generic;
using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Gráfico de LÍNEAS 3D para series temporales (valor por año): coloca un marcador por punto
    /// a la altura de su valor y los une con segmentos. Ideal para "evolución" o "tendencia".
    /// Cada marcador es seleccionable (reutiliza BarraDato en modo marcador).
    /// </summary>
    public class GraficoLineas3D : MonoBehaviour, IGrafico3D
    {
        public BarraDato prefabBarra;
        public float separacion = 1.4f;
        public float alturaMaxima = 2.6f;
        public float alturaBase = 1.2f;
        public Material matLinea;
        public TextMesh titulo, ejeCategoria, ejeValor;
        public Transform piso;
        public int divisionesGrilla = 4;
        public Color color = new Color(0.25f, 0.65f, 0.95f);

        readonly List<GameObject> objetos = new List<GameObject>();

        public bool Dibujar(Resultado r, PlanConsulta plan)
        {
            Limpiar();
            if (r == null || r.filas.Count < 2) return false;

            double max = double.MinValue;
            foreach (var f in r.filas) if (f.valor > max) max = f.valor;
            if (max <= 0) max = 1;

            int n = r.filas.Count;
            float anchoTotal = (n - 1) * separacion;
            var puntos = new Vector3[n];

            for (int i = 0; i < n; i++)
            {
                var fila = r.filas[i];
                float x = i * separacion - anchoTotal / 2f;
                float y = (float)(fila.valor / max) * alturaMaxima + alturaBase;
                puntos[i] = new Vector3(x, y, 0f);

                var marca = Instantiate(prefabBarra, transform);
                marca.gameObject.SetActive(true);
                marca.transform.localPosition = new Vector3(x, 0f, 0f);
                marca.ConfigurarMarcador(fila.etiqueta, fila.valor, y, color, GraficoBarras3D.Formato(fila.valor));
                objetos.Add(marca.gameObject);
            }

            for (int i = 0; i < n - 1; i++) Segmento(puntos[i], puntos[i + 1]);
            DibujarGrilla(max, anchoTotal);

            if (titulo != null) titulo.text = plan != null && !string.IsNullOrEmpty(plan.titulo) ? plan.titulo : "Tendencia";
            if (ejeCategoria != null) ejeCategoria.text = plan != null && !string.IsNullOrEmpty(plan.eje_categoria) ? plan.eje_categoria : r.tituloEtiqueta;
            if (ejeValor != null) ejeValor.text = plan != null && !string.IsNullOrEmpty(plan.eje_valor) ? plan.eje_valor : r.tituloValor;
            if (piso != null) { piso.localScale = new Vector3((anchoTotal + separacion) / 10f, 1f, 0.3f); piso.localPosition = new Vector3(0, -0.01f, 0); }
            return true;
        }

        void Segmento(Vector3 a, Vector3 b)
        {
            var cil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(cil.GetComponent<Collider>());
            cil.name = "Segmento";
            cil.transform.SetParent(transform);
            Vector3 medio = (a + b) / 2f;
            cil.transform.localPosition = medio;
            float largo = Vector3.Distance(a, b);
            cil.transform.localScale = new Vector3(0.06f, largo / 2f, 0.06f);
            cil.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            if (matLinea != null) cil.GetComponent<Renderer>().sharedMaterial = matLinea;
            objetos.Add(cil);
        }

        void DibujarGrilla(double max, float ancho)
        {
            if (divisionesGrilla < 1) return;
            float x0 = -ancho / 2f - separacion * 0.5f;
            for (int i = 0; i <= divisionesGrilla; i++)
            {
                float f = i / (float)divisionesGrilla;
                float y = f * alturaMaxima + alturaBase;
                var linea = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(linea.GetComponent<Collider>());
                linea.name = "Grilla"; linea.transform.SetParent(transform);
                linea.transform.localPosition = new Vector3(0f, y, 0.2f);
                linea.transform.localScale = new Vector3(ancho + separacion, 0.01f, 0.01f);
                if (matLinea != null) linea.GetComponent<Renderer>().sharedMaterial = matLinea;
                objetos.Add(linea);
                objetos.Add(Texto(GraficoBarras3D.Formato(max * f), new Vector3(x0 - 0.1f, y, 0.2f)));
            }
        }

        GameObject Texto(string s, Vector3 pos)
        {
            var g = new GameObject("ValorEje"); g.transform.SetParent(transform);
            g.transform.localPosition = pos; g.transform.localScale = Vector3.one * 0.22f;
            var tm = g.AddComponent<TextMesh>();
            tm.text = s; tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 48; tm.characterSize = 0.1f; tm.anchor = TextAnchor.MiddleRight; tm.alignment = TextAlignment.Right;
            tm.color = new Color(0.7f, 0.78f, 0.9f);
            g.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            g.AddComponent<Texto3D>(); g.AddComponent<MiraACamara>();
            return g;
        }

        public void Limpiar()
        {
            foreach (var o in objetos) if (o != null) Destroy(o);
            objetos.Clear();
        }

        public void Mostrar(bool v) { gameObject.SetActive(v); }
    }
}
