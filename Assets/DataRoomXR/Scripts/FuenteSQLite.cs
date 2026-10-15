// Base de datos SQLite REAL. Se compila SOLO si defines el símbolo de compilación USAR_SQLITE
// (Project Settings ▸ Player ▸ Scripting Define Symbols) y agregas el plugin de SQLite.
// Ver README, sección "Activar SQLite real". Sin el define, el proyecto usa FuenteMemoria y corre igual.
#if USAR_SQLITE
using System;
using System.IO;
using Mono.Data.Sqlite;
using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Fuente de datos sobre una base SQLite real (archivo junin.db en persistentDataPath).
    /// La crea y la llena con el dataset ficticio de Junín en el primer arranque.
    /// Ejecuta SOLO consultas SELECT (PRAGMA query_only) para que la IA no pueda modificar datos.
    /// </summary>
    public class FuenteSQLite : MonoBehaviour, IFuenteDatos
    {
        public string archivo = "junin.db";
        string conexionStr;
        public string Esquema { get; private set; }

        void Awake()
        {
            string ruta = Path.Combine(Application.persistentDataPath, archivo);
            conexionStr = "URI=file:" + ruta;
            if (!File.Exists(ruta) || !HayTabla("medicion")) Crear();
            Esquema = LeerEsquema();
        }

        bool HayTabla(string n)
        {
            try
            {
                using (var c = new SqliteConnection(conexionStr))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@n";
                        cmd.Parameters.Add(new SqliteParameter("@n", n));
                        return cmd.ExecuteScalar() != null;
                    }
                }
            }
            catch { return false; }
        }

        void Crear()
        {
            using (var c = new SqliteConnection(conexionStr))
            {
                c.Open();
                Ej(c, @"DROP TABLE IF EXISTS medicion; DROP TABLE IF EXISTS distrito; DROP TABLE IF EXISTS indicador;
CREATE TABLE distrito(id INTEGER PRIMARY KEY, nombre TEXT, provincia TEXT, poblacion INTEGER);
CREATE TABLE indicador(id INTEGER PRIMARY KEY, nombre TEXT, unidad TEXT);
CREATE TABLE medicion(id INTEGER PRIMARY KEY, distrito_id INTEGER, indicador_id INTEGER, anio INTEGER, valor REAL);");

                using (var tx = c.BeginTransaction())
                {
                    foreach (var d in Semilla.ListaDistritos())
                        P(c, "INSERT INTO distrito VALUES(@a,@b,@c,@d)", ("@a", d.id), ("@b", d.nombre), ("@c", d.provincia), ("@d", d.poblacion));
                    foreach (var i in Semilla.ListaIndicadores())
                        P(c, "INSERT INTO indicador VALUES(@a,@b,@c)", ("@a", i.id), ("@b", i.nombre), ("@c", i.unidad));
                    foreach (var m in Semilla.ListaMediciones())
                        P(c, "INSERT INTO medicion VALUES(@a,@b,@c,@d,@e)", ("@a", m.id), ("@b", m.distrito_id), ("@c", m.indicador_id), ("@d", m.anio), ("@e", m.valor));
                    tx.Commit();
                }
                Debug.Log("[DataRoomXR] BD SQLite creada: " + conexionStr);
            }
        }

        string LeerEsquema()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                using (var c = new SqliteConnection(conexionStr))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND sql IS NOT NULL";
                        using (var r = cmd.ExecuteReader()) while (r.Read()) sb.AppendLine(r.GetString(0)).AppendLine();
                    }
                }
                sb.AppendLine("-- indicador.nombre: Matrícula escolar, Establecimientos de salud, Cobertura de internet, Producción de papa, Reciclaje de residuos.");
                sb.AppendLine("-- medicion.anio: 2021 a 2025.");
            }
            catch (Exception e) { sb.AppendLine("-- error: " + e.Message); }
            return sb.ToString();
        }

        public Resultado Consultar(string sql, int maxFilas = 24)
        {
            var res = new Resultado { sql = sql };
            string err = SeguridadSQL.Validar(sql);
            if (err != null) { res.error = err; return res; }
            try
            {
                using (var c = new SqliteConnection(conexionStr))
                {
                    c.Open();
                    using (var pr = c.CreateCommand()) { pr.CommandText = "PRAGMA query_only=ON;"; pr.ExecuteNonQuery(); }
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = sql; cmd.CommandTimeout = 10;
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.FieldCount >= 1) res.tituloEtiqueta = r.GetName(0);
                            if (r.FieldCount >= 2) res.tituloValor = r.GetName(1);
                            int n = 0;
                            while (r.Read() && n < maxFilas)
                            {
                                string et = r.IsDBNull(0) ? "(sin dato)" : Convert.ToString(r.GetValue(0));
                                double v = 0;
                                if (r.FieldCount >= 2 && !r.IsDBNull(1)) double.TryParse(Convert.ToString(r.GetValue(1)), out v);
                                res.filas.Add(new Fila(et, v)); n++;
                            }
                        }
                    }
                }
            }
            catch (Exception e) { res.error = "SQL inválido: " + e.Message; }
            return res;
        }

        static void Ej(SqliteConnection c, string sql) { using (var cmd = c.CreateCommand()) { cmd.CommandText = sql; cmd.ExecuteNonQuery(); } }
        static void P(SqliteConnection c, string sql, params (string, object)[] ps)
        {
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var (k, v) in ps) cmd.Parameters.Add(new SqliteParameter(k, v));
                cmd.ExecuteNonQuery();
            }
        }
    }
}
#endif
