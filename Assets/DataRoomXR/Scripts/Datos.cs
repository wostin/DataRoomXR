using System;
using System.Collections.Generic;

namespace DataRoomXR
{
    /// <summary>Una fila del resultado: una etiqueta (ej. distrito) y un valor numérico.</summary>
    public struct Fila
    {
        public string etiqueta;
        public double valor;
        public Fila(string e, double v) { etiqueta = e; valor = v; }
    }

    /// <summary>Resultado de una consulta ya resumido para graficar.</summary>
    public class Resultado
    {
        public string tituloEtiqueta = "Categoría";
        public string tituloValor = "Valor";
        public List<Fila> filas = new List<Fila>();
        public string sql = "";
        public string error = "";
        public bool Ok => string.IsNullOrEmpty(error) && filas.Count > 0;
    }

    /// <summary>Lo que la IA devuelve: la consulta SQL y cómo rotular el gráfico.</summary>
    [Serializable]
    public class PlanConsulta
    {
        public string sql;
        public string titulo;
        public string eje_categoria;
        public string eje_valor;
        public string explicacion;
        public string grafico = "barras";   // "barras" | "lineas" | "mapa"
    }

    /// <summary>Resumen estadístico local de un resultado (para el "insight" automático).</summary>
    public class Insight
    {
        public string lider; public double liderValor;
        public string menor; public double menorValor;
        public double promedio, total;
        public double variacionPct;      // variación del primero al último (series temporales)
        public bool esSerieTemporal;
        public string unidad = "";

        public static Insight De(Resultado r)
        {
            var ins = new Insight();
            if (r == null || r.filas.Count == 0) return ins;
            ins.liderValor = double.MinValue; ins.menorValor = double.MaxValue;
            foreach (var f in r.filas)
            {
                ins.total += f.valor;
                if (f.valor > ins.liderValor) { ins.liderValor = f.valor; ins.lider = f.etiqueta; }
                if (f.valor < ins.menorValor) { ins.menorValor = f.valor; ins.menor = f.etiqueta; }
            }
            ins.promedio = ins.total / r.filas.Count;
            // ¿las etiquetas son años? -> serie temporal
            ins.esSerieTemporal = r.filas.Count >= 2 && EsAnio(r.filas[0].etiqueta) && EsAnio(r.filas[r.filas.Count - 1].etiqueta);
            if (ins.esSerieTemporal)
            {
                double ini = r.filas[0].valor, fin = r.filas[r.filas.Count - 1].valor;
                ins.variacionPct = ini != 0 ? (fin - ini) / ini * 100.0 : 0;
            }
            return ins;
        }

        static bool EsAnio(string s) => int.TryParse(s, out int a) && a >= 1900 && a <= 2100;
    }
}
