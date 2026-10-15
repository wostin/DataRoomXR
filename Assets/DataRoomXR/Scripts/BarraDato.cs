using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Una barra (columna) del gráfico 3D. Guarda su etiqueta y su valor, crece con animación
    /// hasta su altura final y se puede resaltar al apuntarla/mirarla. Lleva encima dos TextMesh:
    /// el nombre de la categoría (abajo) y el valor (arriba).
    /// </summary>
    public class BarraDato : MonoBehaviour
    {
        public Transform columna;      // el cubo que se escala en Y
        public TextMesh etiqueta;      // nombre (distrito / año)
        public TextMesh valorTxt;      // número encima
        public Renderer render;

        public string Etiqueta { get; private set; }
        public double Valor { get; private set; }

        float alturaObjetivo;
        float alturaActual;
        Color color;
        MaterialPropertyBlock mpb;
        const float grosor = 0.6f;

        /// <summary>true = punto de una línea / marcador en un mapa (no crece desde el piso).</summary>
        public bool EsMarcador { get; private set; }
        public float grosorMarcador = 0.28f;

        public void Configurar(string etq, double val, float altura, Color c, string valorFormateado)
        {
            EsMarcador = false;
            Etiqueta = etq; Valor = val; alturaObjetivo = Mathf.Max(0.02f, altura); color = c;
            if (etiqueta != null) etiqueta.text = etq;
            if (valorTxt != null) valorTxt.text = valorFormateado;
            Pintar(false);
            alturaActual = 0f;
            Aplicar();
        }

        /// <summary>Configura la barra como un marcador fijo a una altura dada (gráfico de líneas / mapa).</summary>
        public void ConfigurarMarcador(string etq, double val, float alturaMundo, Color c, string valorFormateado)
        {
            EsMarcador = true;
            Etiqueta = etq; Valor = val; alturaObjetivo = alturaMundo; color = c;
            if (etiqueta != null) etiqueta.text = etq;
            if (valorTxt != null) valorTxt.text = valorFormateado;
            Pintar(false);
            alturaActual = alturaMundo;   // sin animación de crecimiento
            Aplicar();
        }

        /// <summary>Altura (en coordenadas locales) donde está el punto; la usa la línea para unir marcadores.</summary>
        public float AlturaPunto => alturaActual;

        void Update()
        {
            if (Mathf.Abs(alturaActual - alturaObjetivo) > 0.001f)
            {
                alturaActual = Mathf.Lerp(alturaActual, alturaObjetivo, Time.deltaTime * 6f);
                Aplicar();
            }
        }

        void Aplicar()
        {
            if (columna == null) return;
            if (EsMarcador)
            {
                columna.localScale = Vector3.one * grosorMarcador;
                columna.localPosition = new Vector3(0f, alturaActual, 0f);
                if (valorTxt != null) valorTxt.transform.localPosition = new Vector3(0f, alturaActual + 0.3f, 0f);
            }
            else
            {
                columna.localScale = new Vector3(grosor, alturaActual, grosor);
                columna.localPosition = new Vector3(0f, alturaActual / 2f, 0f);   // crece desde el piso
                if (valorTxt != null) valorTxt.transform.localPosition = new Vector3(0f, alturaActual + 0.25f, 0f);
            }
        }

        public void Resaltar(bool r)
        {
            Pintar(r);
            if (valorTxt != null) valorTxt.gameObject.SetActive(r || alturaObjetivo > 0f);
        }

        void Pintar(bool resaltado)
        {
            if (render == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Color c = resaltado ? Color.Lerp(color, Color.white, 0.5f) : color;
            render.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);   // URP
            mpb.SetColor("_Color", c);       // Built-in
            render.SetPropertyBlock(mpb);
        }
    }
}
