namespace DataRoomXR
{
    /// <summary>
    /// Cualquier visualización 3D (barras, líneas, mapa) implementa esto, así el
    /// DataRoomManager puede cambiar de tipo de gráfico sin conocer los detalles.
    /// </summary>
    public interface IGrafico3D
    {
        /// <summary>Dibuja el resultado. Devuelve false si este gráfico no aplica a estos datos
        /// (p. ej. el mapa cuando las etiquetas no son distritos), para que el manager use otro.</summary>
        bool Dibujar(Resultado r, PlanConsulta plan);
        void Limpiar();
        void Mostrar(bool visible);
    }
}
