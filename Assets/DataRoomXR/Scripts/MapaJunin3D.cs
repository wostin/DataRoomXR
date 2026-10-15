using System.Collections.Generic;
using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Mapa 3D esquemático de la región Junín: coloca una columna sobre la posición (aproximada)
    /// de cada distrito, con altura proporcional a su valor (tipo "coropleta en 3D").
    /// Solo aplica cuando las etiquetas del resultado son distritos; si no, devuelve false
    /// y el manager usa el gráfico de barras.
    /// </summary>
    public class MapaJunin3D : MonoBehaviour, IGrafico3D
    {
        public BarraDato prefabBarra;
        public Transform baseMapa;      // plano del mapa
        public TextMesh titulo;
        public float escala = 1.1f;     // separación entre distritos
        public float alturaMaxima = 3.5f;
        public Color colorBajo = new Color(0.20f, 0.42f, 0.78f);
        public Color colorAlto = new Color(0.95f, 0.55f, 0.15f);

        readonly List<GameObject> objetos = new List<GameObject>();

        // Posiciones esquemáticas (x,z) relativas de cada distrito en el valle del Mantaro y alrededores.
        // No son coordenadas geográficas exactas; sirven para ubicar las columnas en el mapa.
        static readonly Dictionary<string, Vector2> Coord = new Dictionary<string, Vector2>(System.StringComparer.OrdinalIgnoreCase)
        {
            {"El Tambo",    new Vector2( 0.5f,  0.5f)},
            {"Huancayo",    new Vector2( 0.3f, -0.4f)},
            {"Chilca",      new Vector2( 0.4f, -1.3f)},
            {"Pilcomayo",   new Vector2(-0.8f,  0.6f)},
            {"Concepción",  new Vector2(-0.3f,  2.6f)},
            {"Jauja",       new Vector2(-0.6f,  4.2f)},
            {"Tarma",       new Vector2(-1.8f,  6.6f)},
            {"La Oroya",    new Vector2(-3.6f,  7.6f)},
            {"Satipo",      new Vector2( 5.2f,  3.4f)},
            {"Chanchamayo", new Vector2( 3.2f,  6.0f)},
        };

        public bool Dibujar(Resultado r, PlanConsulta plan)
        {
            Limpiar();
            if (r == null || r.filas.Count == 0) return false;

            int reconocidos = 0;
            foreach (var f in r.filas) if (Coord.ContainsKey(f.etiqueta.Trim())) reconocidos++;
            if (reconocidos < Mathf.Max(2, r.filas.Count / 2)) return false;   // no son distritos → no aplica

            double max = double.MinValue, min = double.MaxValue;
            foreach (var f in r.filas) { if (f.valor > max) max = f.valor; if (f.valor < min) min = f.valor; }
            if (max <= 0) max = 1;
            double rango = (max - min) < 1e-9 ? max : (max - min);

            foreach (var f in r.filas)
            {
                if (!Coord.TryGetValue(f.etiqueta.Trim(), out Vector2 c)) continue;
                var col = Instantiate(prefabBarra, transform);
                col.gameObject.SetActive(true);
                col.transform.localPosition = new Vector3(c.x * escala, 0f, c.y * escala);
                float altura = (float)(f.valor / max) * alturaMaxima;
                float t = (float)((f.valor - min) / rango);
                col.Configurar(f.etiqueta, f.valor, altura, Color.Lerp(colorBajo, colorAlto, t), GraficoBarras3D.Formato(f.valor));
                if (col.etiqueta != null)
                {
                    col.etiqueta.transform.localScale *= 1.7f;
                    col.etiqueta.transform.localPosition += new Vector3(0f, 0.25f, 0f);
                }
                objetos.Add(col.gameObject);
            }

            if (titulo != null)
            {
                titulo.text = (plan != null && !string.IsNullOrEmpty(plan.titulo) ? plan.titulo : "Mapa de Junín") + " (mapa)";
                titulo.transform.localPosition = new Vector3(0f, 4.2f, 2.5f);
            }
            if (baseMapa != null)
            {
                baseMapa.localScale = new Vector3(1.6f, 1f, 1.6f);
                baseMapa.localPosition = new Vector3(0.2f, -0.02f, 2.5f);
            }
            return true;
        }

        public void Limpiar()
        {
            foreach (var o in objetos) if (o != null) Destroy(o);
            objetos.Clear();
        }

        public void Mostrar(bool v) { gameObject.SetActive(v); }
    }
}
