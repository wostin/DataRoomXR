namespace DataRoomXR
{
    /// <summary>
    /// Fuente de datos para las consultas. Hay dos implementaciones:
    ///  - FuenteMemoria: pura en C#, funciona apenas abres el proyecto (sin plugins). [por defecto]
    ///  - FuenteSQLite: base de datos SQLite real (requiere el plugin, ver README). [#if USAR_SQLITE]
    /// Ambas reciben el MISMO SQL generado por la IA y devuelven el mismo tipo de Resultado,
    /// así el resto del proyecto no cambia.
    /// </summary>
    public interface IFuenteDatos
    {
        string Esquema { get; }
        Resultado Consultar(string sql, int maxFilas = 24);
    }
}
