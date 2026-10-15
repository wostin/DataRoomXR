using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Fuente de datos PURA EN C# (sin plugins): funciona apenas abres el proyecto.
    /// Carga el dataset ficticio de Junín en memoria e interpreta la consulta SELECT que
    /// genera la IA (o el traductor por reglas) sobre este esquema fijo.
    /// Soporta: SELECT de 2 columnas (etiqueta, valor), JOINs del modelo, WHERE con AND
    /// (=, !=, <, >, <=, >=, LIKE), SUM/AVG/COUNT/MAX/MIN, ROUND(...), GROUP BY, ORDER BY y LIMIT.
    /// Para ejecutar SQL arbitrario, usa la base SQLite real (ver README, define USAR_SQLITE).
    /// </summary>
    public class FuenteMemoria : MonoBehaviour, IFuenteDatos
    {
        public string Esquema => Semilla.EsquemaTexto;

        List<Dictionary<string, object>> vistaMedicion;  // medicion + distrito + indicador
        List<Dictionary<string, object>> vistaDistrito;   // solo distrito

        void Awake() { Construir(); }

        void Construir()
        {
            var dist = Semilla.ListaDistritos();
            var ind = Semilla.ListaIndicadores();
            var med = Semilla.ListaMediciones();
            var dById = new Dictionary<int, Distrito>(); foreach (var d in dist) dById[d.id] = d;
            var iById = new Dictionary<int, Indicador>(); foreach (var i in ind) iById[i.id] = i;

            vistaMedicion = new List<Dictionary<string, object>>();
            foreach (var m in med)
            {
                var d = dById[m.distrito_id]; var i = iById[m.indicador_id];
                vistaMedicion.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    {"valor", m.valor}, {"anio", m.anio},
                    {"distrito", d.nombre}, {"nombre", d.nombre}, {"provincia", d.provincia}, {"poblacion", d.poblacion},
                    {"indicador", i.nombre}, {"unidad", i.unidad},
                    {"distrito_id", d.id}, {"indicador_id", i.id},
                });
            }
            vistaDistrito = new List<Dictionary<string, object>>();
            foreach (var d in dist)
                vistaDistrito.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    {"nombre", d.nombre}, {"distrito", d.nombre}, {"provincia", d.provincia}, {"poblacion", d.poblacion}, {"id", d.id},
                });
        }

        public Resultado Consultar(string sql, int maxFilas = 24)
        {
            var res = new Resultado { sql = sql };
            try
            {
                if (vistaMedicion == null) Construir();
                Interpretar(sql, maxFilas, res);
            }
            catch (Exception e)
            {
                res.error = "No pude interpretar la consulta en modo memoria (" + e.Message +
                            "). Para SQL libre, activa la base SQLite real (README).";
            }
            if (res.error == null && res.filas.Count == 0) res.error = "La consulta no devolvió filas.";
            return res;
        }

        // ----------------- interpretador -----------------

        class Expr { public string raw, alias, agg, col; public bool round; }

        void Interpretar(string sqlOrig, int maxFilas, Resultado res)
        {
            string sql = sqlOrig.Trim().TrimEnd(';').Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
            while (sql.Contains("  ")) sql = sql.Replace("  ", " ");

            var mSel = Regex.Match(sql, @"^\s*select\s+(.*?)\s+from\s+(.*)$", RegexOptions.IgnoreCase);
            if (!mSel.Success) throw new Exception("no es un SELECT reconocible");
            string colsParte = mSel.Groups[1].Value;
            string resto = mSel.Groups[2].Value;

            // separar cláusulas
            string whereTxt = Sacar(ref resto, @"\bwhere\b(.*?)(?=\bgroup\b|\border\b|\blimit\b|$)");
            string groupTxt = Sacar(ref resto, @"\bgroup\s+by\b(.*?)(?=\border\b|\blimit\b|$)");
            string orderTxt = Sacar(ref resto, @"\border\s+by\b(.*?)(?=\blimit\b|$)");
            string limitTxt = Sacar(ref resto, @"\blimit\b\s+(\d+)");
            string fromTxt = resto; // lo que queda es FROM ... (con joins)

            var exprs = ParsearColumnas(colsParte);
            if (exprs.Count < 1) throw new Exception("sin columnas");

            // elegir vista base
            bool usaMedicion = Regex.IsMatch(fromTxt + " " + whereTxt + " " + colsParte, @"\bmedicion\b|\bm\.", RegexOptions.IgnoreCase)
                               || Regex.IsMatch(colsParte + " " + whereTxt, @"\bvalor\b|\banio\b|\bindicador\b", RegexOptions.IgnoreCase);
            var filas = usaMedicion ? Clonar(vistaMedicion) : Clonar(vistaDistrito);

            // WHERE
            if (!string.IsNullOrWhiteSpace(whereTxt))
            {
                var conds = ParsearWhere(whereTxt);
                filas = filas.FindAll(r => conds.TrueForAll(c => c.Cumple(r)));
            }

            bool hayAgg = exprs.Exists(e => e.agg != null);
            var salida = new List<Dictionary<string, object>>();

            if (!string.IsNullOrWhiteSpace(groupTxt) || hayAgg)
            {
                var claves = new List<string>();
                if (!string.IsNullOrWhiteSpace(groupTxt))
                    foreach (var g in groupTxt.Split(',')) claves.Add(Norm(g.Trim()));
                else
                    foreach (var e in exprs) if (e.agg == null) claves.Add(e.col);

                var grupos = new Dictionary<string, List<Dictionary<string, object>>>();
                var orden = new List<string>();
                foreach (var r in filas)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var k in claves) sb.Append(Val(r, k)).Append('|');
                    string key = sb.ToString();
                    if (!grupos.ContainsKey(key)) { grupos[key] = new List<Dictionary<string, object>>(); orden.Add(key); }
                    grupos[key].Add(r);
                }
                foreach (var key in orden)
                {
                    var g = grupos[key];
                    var fila = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var e in exprs) fila[e.alias] = Evaluar(e, g);
                    salida.Add(fila);
                }
            }
            else
            {
                foreach (var r in filas)
                {
                    var fila = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var e in exprs) fila[e.alias] = e.col != null ? Val(r, e.col) : null;
                    salida.Add(fila);
                }
            }

            // ORDER BY
            if (!string.IsNullOrWhiteSpace(orderTxt))
            {
                var partes = orderTxt.Trim().Split(' ');
                string clave = Norm(partes[0]);
                bool desc = orderTxt.IndexOf("desc", StringComparison.OrdinalIgnoreCase) >= 0;
                salida.Sort((a, b) =>
                {
                    object va = Buscar(a, clave, exprs), vb = Buscar(b, clave, exprs);
                    int cmp = Comparar(va, vb);
                    return desc ? -cmp : cmp;
                });
            }

            // LIMIT
            int limite = maxFilas;
            if (!string.IsNullOrWhiteSpace(limitTxt) && int.TryParse(limitTxt.Trim(), out int lim)) limite = Math.Min(limite, lim);

            res.tituloEtiqueta = exprs[0].alias;
            res.tituloValor = exprs.Count > 1 ? exprs[1].alias : "valor";
            for (int i = 0; i < salida.Count && i < limite; i++)
            {
                var r = salida[i];
                string et = Convert.ToString(exprs[0].col != null || exprs[0].agg != null ? r[exprs[0].alias] : r[exprs[0].alias], CultureInfo.InvariantCulture);
                double val = 0;
                if (exprs.Count > 1) ADouble(r[exprs[1].alias], out val);
                res.filas.Add(new Fila(et, val));
            }
        }

        // ----------------- helpers -----------------

        static string Sacar(ref string resto, string patron)
        {
            var m = Regex.Match(resto, patron, RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            resto = resto.Remove(m.Index, m.Length);
            return m.Groups[m.Groups.Count - 1].Value.Trim();
        }

        List<Expr> ParsearColumnas(string s)
        {
            var res = new List<Expr>();
            foreach (var parte in DividirComas(s))
            {
                string p = parte.Trim();
                var e = new Expr { raw = p };
                var mAlias = Regex.Match(p, @"^(.*?)\s+as\s+([A-Za-z_][\w]*)$", RegexOptions.IgnoreCase);
                string cuerpo = p;
                if (mAlias.Success) { cuerpo = mAlias.Groups[1].Value.Trim(); e.alias = mAlias.Groups[2].Value; }

                string c = cuerpo;
                var mRound = Regex.Match(c, @"^round\s*\((.*)\)$", RegexOptions.IgnoreCase);
                if (mRound.Success) { e.round = true; c = mRound.Groups[1].Value.Trim();
                    // ROUND(x, n) -> quitar el , n
                    int coma = c.LastIndexOf(','); if (coma > 0 && c.IndexOf('(') < 0) c = c.Substring(0, coma).Trim();
                }
                var mAgg = Regex.Match(c, @"^(sum|avg|count|max|min)\s*\((.*)\)$", RegexOptions.IgnoreCase);
                if (mAgg.Success) { e.agg = mAgg.Groups[1].Value.ToLowerInvariant(); c = mAgg.Groups[2].Value.Trim(); }

                e.col = c == "*" ? null : Norm(c);
                if (e.alias == null) e.alias = e.agg != null ? (e.agg + (e.col ?? "")) : (e.col ?? "col");
                res.Add(e);
            }
            return res;
        }

        class Cond { public string col, op; public object val; public bool like;
            public bool Cumple(Dictionary<string, object> r)
            {
                object v = r.ContainsKey(col) ? r[col] : null;
                if (like) return Convert.ToString(v, CultureInfo.InvariantCulture).IndexOf(Convert.ToString(val).Trim('%'), StringComparison.OrdinalIgnoreCase) >= 0;
                if (double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out double a) &&
                    double.TryParse(Convert.ToString(val, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out double b))
                    return CompNum(a, b, op);
                string sa = Convert.ToString(v, CultureInfo.InvariantCulture), sb = Convert.ToString(val, CultureInfo.InvariantCulture);
                int c = string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
                return op == "=" ? c == 0 : (op == "!=" || op == "<>") ? c != 0 : CompNum(c, 0, op);
            }
            static bool CompNum(double a, double b, string op)
            {
                switch (op) { case "=": return a == b; case "!=": case "<>": return a != b;
                    case ">": return a > b; case "<": return a < b; case ">=": return a >= b; case "<=": return a <= b; }
                return false;
            }
        }

        List<Cond> ParsearWhere(string s)
        {
            var res = new List<Cond>();
            foreach (var trozo in Regex.Split(s, @"\s+and\s+", RegexOptions.IgnoreCase))
            {
                var t = trozo.Trim();
                if (t.Length == 0) continue;
                var mLike = Regex.Match(t, @"^(.*?)\s+like\s+(.*)$", RegexOptions.IgnoreCase);
                if (mLike.Success) { res.Add(new Cond { col = Norm(mLike.Groups[1].Value), op = "like", like = true, val = Limpiar(mLike.Groups[2].Value) }); continue; }
                var m = Regex.Match(t, @"^(.*?)\s*(>=|<=|<>|!=|=|>|<)\s*(.*)$");
                if (m.Success) res.Add(new Cond { col = Norm(m.Groups[1].Value), op = m.Groups[2].Value, val = Limpiar(m.Groups[3].Value) });
            }
            return res;
        }

        object Evaluar(Expr e, List<Dictionary<string, object>> grupo)
        {
            if (e.agg == null) return grupo.Count > 0 ? Val(grupo[0], e.col) : null;
            if (e.agg == "count") return grupo.Count;
            double acc = e.agg == "min" ? double.MaxValue : e.agg == "max" ? double.MinValue : 0;
            int n = 0;
            foreach (var r in grupo)
            {
                if (!ADouble(Val(r, e.col), out double v)) continue;
                n++;
                switch (e.agg) { case "sum": case "avg": acc += v; break; case "min": acc = Math.Min(acc, v); break; case "max": acc = Math.Max(acc, v); break; }
            }
            if (e.agg == "avg") acc = n > 0 ? acc / n : 0;
            if (e.round) acc = Math.Round(acc);
            return acc;
        }

        static object Buscar(Dictionary<string, object> fila, string clave, List<Expr> exprs)
        {
            if (fila.ContainsKey(clave)) return fila[clave];
            foreach (var e in exprs) if (string.Equals(e.col, clave, StringComparison.OrdinalIgnoreCase) && fila.ContainsKey(e.alias)) return fila[e.alias];
            foreach (var kv in fila) return kv.Value; // última defensa
            return null;
        }

        static object Val(Dictionary<string, object> r, string col) => col != null && r.ContainsKey(col) ? r[col] : null;

        static bool ADouble(object o, out double v) =>
            double.TryParse(Convert.ToString(o, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out v);

        static int Comparar(object a, object b)
        {
            if (ADouble(a, out double da) && ADouble(b, out double db)) return da.CompareTo(db);
            return string.Compare(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resuelve un token de columna a la clave real de la vista plana, RESPETANDO el prefijo de
        /// tabla: 'i.nombre' -> indicador, 'd.nombre' -> distrito, 'm.valor' -> valor, etc.
        /// Un 'nombre' suelto se interpreta como el nombre del distrito.
        /// </summary>
        static string Norm(string c)
        {
            string token = c.Trim().Trim('"', '`', '[', ']').ToLowerInvariant();
            int punto = token.IndexOf('.');
            if (punto >= 0)
            {
                string pre = token.Substring(0, punto).Trim();
                string field = token.Substring(punto + 1).Trim();
                if (pre == "i" || pre == "indicador") return field == "nombre" ? "indicador" : field;
                if (pre == "d" || pre == "distrito") return field == "nombre" ? "distrito" : field;
                if (pre == "m" || pre == "medicion") return field;
                return field;
            }
            return token == "nombre" ? "distrito" : token;
        }

        static object Limpiar(string v)
        {
            v = v.Trim();
            if ((v.StartsWith("'") && v.EndsWith("'")) || (v.StartsWith("\"") && v.EndsWith("\"")))
                return v.Substring(1, v.Length - 2);
            return v;
        }

        static List<string> DividirComas(string s)
        {
            var res = new List<string>(); int prof = 0, ini = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '(') prof++;
                else if (s[i] == ')') prof--;
                else if (s[i] == ',' && prof == 0) { res.Add(s.Substring(ini, i - ini)); ini = i + 1; }
            }
            res.Add(s.Substring(ini));
            return res;
        }

        static List<Dictionary<string, object>> Clonar(List<Dictionary<string, object>> src)
        {
            var r = new List<Dictionary<string, object>>(src.Count);
            foreach (var d in src) r.Add(d); // solo lectura; no hace falta copia profunda
            return r;
        }
    }
}
