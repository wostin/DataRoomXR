using System.Collections.Generic;

namespace DataRoomXR
{
    public class Distrito { public int id; public string nombre; public string provincia; public int poblacion; }
    public class Indicador { public int id; public string nombre; public string unidad; }
    public class Medicion { public int id; public int distrito_id; public int indicador_id; public int anio; public double valor; }

    /// <summary>
    /// Dataset FICTICIO de indicadores de la región Junín, con fines académicos
    /// (los números son inventados, NO son datos reales). Lo usan las dos fuentes de datos.
    /// Para el proyecto final se reemplaza por datos abiertos reales de datosabiertos.gob.pe / INEI.
    ///
    /// Modelo estrella:
    ///   distrito(id, nombre, provincia, poblacion)
    ///   indicador(id, nombre, unidad)
    ///   medicion(id, distrito_id, indicador_id, anio, valor)
    /// </summary>
    public static class Semilla
    {
        public static readonly string[,] Distritos = {
            {"El Tambo","Huancayo","160000"},
            {"Huancayo","Huancayo","120000"},
            {"Chilca","Huancayo","90000"},
            {"Pilcomayo","Huancayo","20000"},
            {"Concepción","Concepción","60000"},
            {"Jauja","Jauja","25000"},
            {"Tarma","Tarma","55000"},
            {"La Oroya","Yauli","30000"},
            {"Satipo","Satipo","45000"},
            {"Chanchamayo","Chanchamayo","40000"},
        };

        public static readonly string[,] Indicadores = {
            {"Matrícula escolar","estudiantes"},
            {"Establecimientos de salud","locales"},
            {"Cobertura de internet","% hogares"},
            {"Producción de papa","toneladas"},
            {"Reciclaje de residuos","toneladas/mes"},
        };

        public const int AnioIni = 2021, AnioFin = 2025;

        public const string EsquemaTexto =
@"CREATE TABLE distrito (id INTEGER PRIMARY KEY, nombre TEXT, provincia TEXT, poblacion INTEGER);
CREATE TABLE indicador (id INTEGER PRIMARY KEY, nombre TEXT, unidad TEXT);
CREATE TABLE medicion (id INTEGER PRIMARY KEY, distrito_id INTEGER REFERENCES distrito(id),
                       indicador_id INTEGER REFERENCES indicador(id), anio INTEGER, valor REAL);
-- Indicadores (indicador.nombre): Matrícula escolar, Establecimientos de salud,
-- Cobertura de internet, Producción de papa, Reciclaje de residuos.
-- Años disponibles en medicion.anio: 2021 a 2025.";

        public static List<Distrito> ListaDistritos()
        {
            var l = new List<Distrito>();
            for (int i = 0; i < Distritos.GetLength(0); i++)
                l.Add(new Distrito { id = i + 1, nombre = Distritos[i, 0], provincia = Distritos[i, 1], poblacion = int.Parse(Distritos[i, 2]) });
            return l;
        }

        public static List<Indicador> ListaIndicadores()
        {
            var l = new List<Indicador>();
            for (int i = 0; i < Indicadores.GetLength(0); i++)
                l.Add(new Indicador { id = i + 1, nombre = Indicadores[i, 0], unidad = Indicadores[i, 1] });
            return l;
        }

        public static List<Medicion> ListaMediciones()
        {
            var l = new List<Medicion>();
            var rnd = new System.Random(2026);
            int id = 1;
            for (int d = 1; d <= Distritos.GetLength(0); d++)
            {
                int pob = int.Parse(Distritos[d - 1, 2]);
                for (int ind = 1; ind <= Indicadores.GetLength(0); ind++)
                {
                    double baseVal = BaseIndicador(ind, pob, rnd);
                    for (int anio = AnioIni; anio <= AnioFin; anio++)
                    {
                        double crece = 1.0 + (anio - AnioIni) * (0.02 + rnd.NextDouble() * 0.06);
                        double ruido = 0.9 + rnd.NextDouble() * 0.2;
                        double valor = System.Math.Round(baseVal * crece * ruido, ind == 3 ? 1 : 0);
                        l.Add(new Medicion { id = id++, distrito_id = d, indicador_id = ind, anio = anio, valor = valor });
                    }
                }
            }
            return l;
        }

        static double BaseIndicador(int ind, int pob, System.Random r)
        {
            switch (ind)
            {
                case 1: return pob * (0.18 + r.NextDouble() * 0.04);
                case 2: return System.Math.Max(2, pob / 9000.0);
                case 3: return 35 + r.NextDouble() * 45;
                case 4: return pob * (0.05 + r.NextDouble() * 0.5);
                case 5: return pob / 2500.0 + r.NextDouble() * 20;
                default: return 100;
            }
        }
    }
}
