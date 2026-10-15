using System.Text.RegularExpressions;

namespace DataRoomXR
{
    /// <summary>
    /// Filtro de seguridad para el SQL que genera la IA: permite UNA sola consulta SELECT
    /// y bloquea cualquier intento de modificar o borrar datos (defensa en profundidad;
    /// además la conexión se abre en modo solo lectura).
    /// </summary>
    public static class SeguridadSQL
    {
        public static string Validar(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return "Consulta vacía.";
            string s = sql.Trim().TrimEnd(';').Trim();
            if (s.IndexOf(';') >= 0) return "Solo se permite una consulta.";
            string low = s.ToLowerInvariant();
            if (!(low.StartsWith("select ") || low.StartsWith("with ")))
                return "Solo se permiten consultas SELECT.";
            string[] prohibidas = { "insert", "update", "delete", "drop", "alter", "create",
                                    "replace", "attach", "detach", "pragma", "vacuum", "reindex", "truncate" };
            foreach (var p in prohibidas)
                if (Regex.IsMatch(low, "\\b" + p + "\\b"))
                    return "Palabra no permitida en la consulta: " + p;
            return null;
        }
    }
}
