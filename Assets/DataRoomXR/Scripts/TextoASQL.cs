using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DataRoomXR
{
    /// <summary>
    /// Convierte una pregunta en lenguaje natural en una consulta SQL (SELECT) usando un LLM.
    /// Le pasa el esquema real de la BD para que la consulta sea válida.
    /// Soporta Anthropic (Claude) y cualquier API compatible con OpenAI (Gemini, Groq, OpenAI).
    /// Si no hay IA configurada, usa TextoASQLReglas (traductor por reglas, sin internet).
    /// </summary>
    public static class TextoASQL
    {
        const string Sistema =
@"Eres un traductor de preguntas en español a consultas SQL para SQLite.
Responde SOLO con JSON válido, sin texto adicional ni ```:
{""sql"":""SELECT ..."",""titulo"":""título corto del gráfico"",""eje_categoria"":""qué representa cada barra"",""eje_valor"":""qué mide la altura"",""grafico"":""barras|lineas|mapa"",""explicacion"":""1 frase de qué muestra""}

REGLAS ESTRICTAS:
- Genera UNA sola sentencia SELECT (o WITH ... SELECT). Nunca INSERT, UPDATE, DELETE, DROP, ALTER, CREATE ni varias sentencias.
- La PRIMERA columna del SELECT es la etiqueta (texto) y la SEGUNDA es el valor numérico a graficar. Usa alias claros.
- Devuelve como máximo 24 filas; usa ORDER BY y LIMIT cuando corresponda.
- Usa SOLO las tablas y columnas del esquema. No inventes nombres.
- Para 'por distrito' agrupa por distrito.nombre; para 'por año' agrupa por medicion.anio.
- Campo 'grafico': usa 'lineas' si la etiqueta es el AÑO (serie temporal); 'mapa' si la pregunta pide un mapa o ubicación y la etiqueta es el distrito; 'barras' en los demás casos.
- Si la pregunta no se puede responder con el esquema, devuelve sql vacío y explica en 'explicacion'.

ESQUEMA DE LA BASE DE DATOS:
";

        public static IEnumerator Traducir(string pregunta, string esquema, Action<PlanConsulta> ok, Action<string> error)
        {
            var cfg = ConfigIA.Cargar();
            if (!cfg.Disponible)
            {
                var plan = TextoASQLReglas.Traducir(pregunta);
                if (plan != null) ok(plan);
                else error("Sin IA configurada y la pregunta no coincide con ninguna plantilla. Prueba con las preguntas sugeridas.");
                yield break;
            }

            string prompt = Sistema + esquema + "\n\nPREGUNTA DEL USUARIO:\n" + pregunta;
            string texto = null, err = null;
            yield return Pedir(cfg, prompt, t => texto = t, e => err = e);

            if (err != null)
            {
                // Si la IA falla, cae al traductor por reglas para no dejar al usuario sin respuesta.
                var plan = TextoASQLReglas.Traducir(pregunta);
                if (plan != null) ok(plan);
                else error(err);
                yield break;
            }

            try
            {
                var plan = JsonUtility.FromJson<PlanConsulta>(Extraer(texto));
                if (plan == null || string.IsNullOrWhiteSpace(plan.sql))
                    error(plan != null && !string.IsNullOrEmpty(plan.explicacion) ? plan.explicacion : "La IA no devolvió una consulta.");
                else ok(plan);
            }
            catch (Exception ex) { error("Respuesta de la IA no es JSON válido: " + ex.Message); }
        }

        static IEnumerator Pedir(ConfigIA cfg, string prompt, Action<string> ok, Action<string> error)
        {
            string url, cuerpo;
            if (cfg.proveedor == ConfigIA.Proveedor.Anthropic)
            {
                url = "https://api.anthropic.com/v1/messages";
                cuerpo = "{\"model\":\"" + Esc(cfg.modelo) + "\",\"max_tokens\":1024," +
                         "\"messages\":[{\"role\":\"user\",\"content\":\"" + Esc(prompt) + "\"}]}";
            }
            else
            {
                url = cfg.urlOpenAI;
                cuerpo = "{\"model\":\"" + Esc(cfg.modelo) + "\",\"temperature\":0.1," +
                         "\"messages\":[{\"role\":\"user\",\"content\":\"" + Esc(prompt) + "\"}]}";
            }

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(cuerpo));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = cfg.timeoutSegundos;
                req.SetRequestHeader("Content-Type", "application/json");
                if (cfg.proveedor == ConfigIA.Proveedor.Anthropic)
                {
                    req.SetRequestHeader("x-api-key", cfg.apiKey);
                    req.SetRequestHeader("anthropic-version", "2023-06-01");
                }
                else req.SetRequestHeader("Authorization", "Bearer " + cfg.apiKey);

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    error("Error de red/API (" + req.responseCode + "): " + Recortar(req.downloadHandler.text, 180));
                    yield break;
                }

                try
                {
                    string resp = req.downloadHandler.text, texto;
                    if (cfg.proveedor == ConfigIA.Proveedor.Anthropic)
                    {
                        var r = JsonUtility.FromJson<RespAnthropic>(resp);
                        var sb = new StringBuilder();
                        if (r.content != null) foreach (var b in r.content) if (b.type == "text") sb.Append(b.text);
                        texto = sb.ToString();
                    }
                    else
                    {
                        var r = JsonUtility.FromJson<RespOpenAI>(resp);
                        texto = r.choices != null && r.choices.Length > 0 ? r.choices[0].message.content : "";
                    }
                    if (string.IsNullOrEmpty(texto)) error("Respuesta vacía de la IA.");
                    else ok(texto);
                }
                catch (Exception ex) { error("No se pudo leer la respuesta: " + ex.Message); }
            }
        }

        [Serializable] class RespAnthropic { public Bloque[] content; }
        [Serializable] class Bloque { public string type; public string text; }
        [Serializable] class RespOpenAI { public Choice[] choices; }
        [Serializable] class Choice { public Msg message; }
        [Serializable] class Msg { public string content; }

        public static string Extraer(string s)
        {
            int a = s.IndexOf('{'), b = s.LastIndexOf('}');
            if (a < 0 || b <= a) throw new Exception("no hay JSON en la respuesta");
            return s.Substring(a, b - a + 1);
        }

        static string Esc(string s)
        {
            var sb = new StringBuilder(s.Length + 16);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        static string Recortar(string s, int n) => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(0, n) + "...");
    }
}
