using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Panel 3D flotante con el título, el estado ("Pensando...", errores), el SQL generado
    /// y los botones de preguntas sugeridas. Se usa en PC y en VR (es geometría 3D).
    /// El texto escrito se maneja por teclado en PC (ver DataRoomManager) y por los botones
    /// sugeridos / voz en VR.
    /// </summary>
    public class PanelConsulta : MonoBehaviour
    {
        public TextMesh textoPregunta;   // lo que el usuario va escribiendo
        public TextMesh textoEstado;     // mensajes de estado / error
        public TextMesh textoSQL;        // el SQL generado (transparencia para el curso)
        public Boton3D[] sugeridas;      // botones de preguntas de ejemplo
        public Boton3D[] historial;      // botones de preguntas recientes (re-ejecutables)

        public void MostrarPregunta(string s)
        {
            if (textoPregunta != null) textoPregunta.text = string.IsNullOrEmpty(s) ? "<escribe tu pregunta y pulsa Enter>" : s;
        }

        public void MostrarEstado(string s, Color c)
        {
            if (textoEstado != null) { textoEstado.text = s; textoEstado.color = c; }
        }

        public void MostrarSQL(string sql)
        {
            if (textoSQL != null) textoSQL.text = string.IsNullOrEmpty(sql) ? "" : "SQL:\n" + sql;
        }

        public void CargarSugeridas(string[] preguntas)
        {
            if (sugeridas == null) return;
            for (int i = 0; i < sugeridas.Length; i++)
            {
                if (sugeridas[i] == null) continue;
                if (i < preguntas.Length) sugeridas[i].Configurar("sug" + i, preguntas[i]);
                else sugeridas[i].Ocultar();
            }
        }

        public void CargarHistorial(string[] preguntas)
        {
            if (historial == null) return;
            for (int i = 0; i < historial.Length; i++)
            {
                if (historial[i] == null) continue;
                if (i < preguntas.Length) historial[i].Configurar("hist" + i, preguntas[i]);
                else historial[i].Ocultar();
            }
        }
    }
}
