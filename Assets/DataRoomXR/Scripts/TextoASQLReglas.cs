using System.Collections.Generic;

namespace DataRoomXR
{
    /// <summary>
    /// Traductor de respaldo por reglas: reconoce frases comunes y arma el SQL sin internet.
    /// Sirve para demostrar el proyecto aunque no haya API de IA, y como "red de seguridad"
    /// si la IA falla. Devuelve null si no entiende la pregunta.
    /// </summary>
    public static class TextoASQLReglas
    {
        // (nombre del indicador tal como está en la tabla, palabras que lo disparan)
        static readonly (string nombre, string[] claves)[] indicadores =
        {
            ("Matrícula escolar",          new[]{"matricula","matrícula","escolar","estudiante","alumno","colegio","educacion","educación"}),
            ("Establecimientos de salud",  new[]{"salud","hospital","posta","establecimiento","centro de salud"}),
            ("Cobertura de internet",      new[]{"internet","conectividad","cobertura","digital","red"}),
            ("Producción de papa",         new[]{"papa","produccion","producción","agricola","agrícola","cultivo","agro"}),
            ("Reciclaje de residuos",      new[]{"reciclaje","residuo","basura","reciclar","ambiental"}),
        };

        public static PlanConsulta Traducir(string pregunta)
        {
            if (string.IsNullOrWhiteSpace(pregunta)) return null;
            string q = pregunta.ToLowerInvariant();

            string indicador = DetectarIndicador(q);
            int anio = DetectarAnio(q);
            bool porAnio = q.Contains("año") || q.Contains("anio") || q.Contains("evolucion") || q.Contains("evolución") ||
                           q.Contains("tendencia") || q.Contains("por año") || q.Contains("cada año");
            bool top = q.Contains("mas ") || q.Contains("más ") || q.Contains("mayor") || q.Contains("top") || q.Contains("ranking");
            bool menor = q.Contains("menos") || q.Contains("menor") || q.Contains("peor");
            bool pideMapa = q.Contains("mapa") || q.Contains("dónde") || q.Contains("donde") || q.Contains("ubica") || q.Contains("geográfic") || q.Contains("geografic");

            // 1) Promedio de cobertura de internet por provincia en 2025
            if ((q.Contains("internet") || q.Contains("cobertura")) && q.Contains("provincia"))
            {
                return new PlanConsulta
                {
                    sql =
@"SELECT d.provincia AS Provincia, ROUND(AVG(m.valor)) AS Promedio
FROM medicion m JOIN indicador i ON i.id=m.indicador_id JOIN distrito d ON d.id=m.distrito_id
WHERE i.nombre='Cobertura de internet' AND m.anio=2025 GROUP BY d.provincia ORDER BY Promedio DESC",
                    titulo = "Promedio de cobertura de internet por provincia en 2025",
                    eje_categoria = "Provincia",
                    eje_valor = "Promedio",
                    grafico = "barras",
                    explicacion = "Promedio de cobertura de internet por provincia en 2025."
                };
            }

            // 2) Mapa de producción de papa por distrito en 2025
            if (q.Contains("papa") && (pideMapa || q.Contains("mapa")))
            {
                return new PlanConsulta
                {
                    sql =
@"SELECT d.nombre AS Distrito, ROUND(m.valor) AS Valor
FROM medicion m JOIN indicador i ON i.id=m.indicador_id JOIN distrito d ON d.id=m.distrito_id
WHERE i.nombre='Producción de papa' AND m.anio=2025 ORDER BY m.valor DESC",
                    titulo = "Mapa de producción de papa por distrito en 2025",
                    eje_categoria = "Distrito",
                    eje_valor = "Valor",
                    grafico = "mapa",
                    explicacion = "Mapa de producción de papa por distrito en 2025."
                };
            }

            // 3) Evolución del reciclaje de residuos en El Tambo por año
            if ((q.Contains("reciclaje") || q.Contains("residuo")) && q.Contains("tambo"))
            {
                return new PlanConsulta
                {
                    sql =
@"SELECT m.anio AS Anio, ROUND(SUM(m.valor)) AS Total
FROM medicion m JOIN indicador i ON i.id=m.indicador_id JOIN distrito d ON d.id=m.distrito_id
WHERE i.nombre='Reciclaje de residuos' AND d.nombre='El Tambo' GROUP BY m.anio ORDER BY m.anio",
                    titulo = "Evolución del reciclaje de residuos en El Tambo por año",
                    eje_categoria = "Año",
                    eje_valor = "Total",
                    grafico = "lineas",
                    explicacion = "Evolución del reciclaje de residuos en El Tambo por año."
                };
            }

            // 4) Matrícula escolar total por provincia en 2025
            if ((q.Contains("matricula") || q.Contains("matrícula") || q.Contains("escolar")) && q.Contains("provincia"))
            {
                return new PlanConsulta
                {
                    sql =
@"SELECT d.provincia AS Provincia, ROUND(SUM(m.valor)) AS Total
FROM medicion m JOIN indicador i ON i.id=m.indicador_id JOIN distrito d ON d.id=m.distrito_id
WHERE i.nombre='Matrícula escolar' AND m.anio=2025 GROUP BY d.provincia ORDER BY Total DESC",
                    titulo = "Matrícula escolar total por provincia en 2025",
                    eje_categoria = "Provincia",
                    eje_valor = "Total",
                    grafico = "barras",
                    explicacion = "Matrícula escolar total por provincia en 2025."
                };
            }

            // Evolución por año de un indicador (sumado en toda la región o de un distrito)
            if (porAnio && indicador != null)
            {
                string distrito = DetectarDistrito(q);
                string filtroDist = distrito != null ? $" AND d.nombre='{distrito}'" : "";
                return new PlanConsulta
                {
                    sql =
$@"SELECT m.anio AS Anio, ROUND(SUM(m.valor)) AS Total
FROM medicion m
JOIN indicador i ON i.id=m.indicador_id
JOIN distrito d ON d.id=m.distrito_id
WHERE i.nombre='{indicador}'{filtroDist}
GROUP BY m.anio ORDER BY m.anio",
                    titulo = indicador + (distrito != null ? " en " + distrito : " en Junín") + " por año",
                    eje_categoria = "Año",
                    eje_valor = indicador,
                    grafico = "lineas",
                    explicacion = "Evolución de " + indicador.ToLower() + " entre 2021 y 2025."
                };
            }

            // Ranking de distritos por un indicador en un año
            if (indicador != null)
            {
                string orden = menor ? "ASC" : "DESC";
                return new PlanConsulta
                {
                    sql =
$@"SELECT d.nombre AS Distrito, ROUND(m.valor) AS Valor
FROM medicion m
JOIN indicador i ON i.id=m.indicador_id
JOIN distrito d ON d.id=m.distrito_id
WHERE i.nombre='{indicador}' AND m.anio={anio}
ORDER BY m.valor {orden} LIMIT 10",
                    titulo = indicador + " por distrito (" + anio + ")",
                    eje_categoria = "Distrito",
                    eje_valor = indicador,
                    grafico = pideMapa ? "mapa" : "barras",
                    explicacion = (menor ? "Distritos con menor " : "Distritos con mayor ") + indicador.ToLower() + " en " + anio + "."
                };
            }

            // Población por distrito (no necesita indicador)
            if (q.Contains("poblacion") || q.Contains("población") || q.Contains("habitante"))
            {
                return new PlanConsulta
                {
                    sql = "SELECT nombre AS Distrito, poblacion AS Poblacion FROM distrito ORDER BY poblacion DESC LIMIT 10",
                    titulo = "Población por distrito",
                    eje_categoria = "Distrito",
                    eje_valor = "Población",
                    grafico = pideMapa ? "mapa" : "barras",
                    explicacion = "Población estimada de cada distrito."
                };
            }

            return null;
        }

        static string DetectarIndicador(string q)
        {
            foreach (var (nombre, claves) in indicadores)
                foreach (var c in claves)
                    if (q.Contains(c)) return nombre;
            return null;
        }

        static int DetectarAnio(string q)
        {
            for (int a = 2021; a <= 2025; a++) if (q.Contains(a.ToString())) return a;
            return 2025; // por defecto, el último año
        }

        static readonly string[] distritos =
        { "El Tambo","Huancayo","Chilca","Pilcomayo","Concepción","Jauja","Tarma","La Oroya","Satipo","Chanchamayo" };

        static string DetectarDistrito(string q)
        {
            foreach (var d in distritos) if (q.Contains(d.ToLowerInvariant())) return d;
            return null;
        }

        /// <summary>Preguntas de ejemplo que se muestran como botones sugeridos.</summary>
        public static string[] Sugeridas()
        {
            return new[]
            {
                "¿Qué distritos tienen más matrícula escolar en 2025?",
                "Mapa de cobertura de internet por distrito en 2024",
                "Evolución de la producción de papa en Junín por año",
                "Población por distrito",
                "¿Qué distrito recicla menos residuos en 2025?",
                "Establecimientos de salud por distrito",
                "Promedio de cobertura de internet por provincia en 2025",
                "Mapa de producción de papa por distrito en 2025",
                "Evolución del reciclaje de residuos en El Tambo por año",
                "Matrícula escolar total por provincia en 2025",
            };
        }
    }
}
