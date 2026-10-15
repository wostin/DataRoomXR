using UnityEngine;

namespace DataRoomXR
{
    /// <summary>
    /// Configuración de la IA. El asset vive en Resources/ConfigIA.asset.
    /// ¡No subas tu API key a GitHub! (agrega Resources/ConfigIA.asset al .gitignore).
    /// Si no la configuras, el proyecto usa un traductor de preguntas por reglas (sin internet).
    /// </summary>
    [CreateAssetMenu(menuName = "DataRoomXR/Config IA", fileName = "ConfigIA")]
    public class ConfigIA : ScriptableObject
    {
        public enum Proveedor { Ninguno, Anthropic, OpenAICompatible }

        [Tooltip("Ninguno = usa el traductor por reglas (preguntas frecuentes), sin internet")]
        public Proveedor proveedor = Proveedor.Ninguno;

        [Tooltip("Tu API key (Anthropic, Google Gemini, Groq u OpenAI)")]
        public string apiKey = "";

        [Tooltip("Anthropic: claude-haiku-5-5 · Gemini: gemini-2.5-flash · Groq: llama-3.3-70b-versatile")]
        public string modelo = "gemini-2.5-flash";

        [Tooltip("Solo OpenAICompatible. Gemini (capa gratuita): https://generativelanguage.googleapis.com/v1beta/openai/chat/completions · Groq: https://api.groq.com/openai/v1/chat/completions")]
        public string urlOpenAI = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";

        public int timeoutSegundos = 40;

        public bool Disponible => proveedor != Proveedor.Ninguno && !string.IsNullOrEmpty(apiKey);

        static ConfigIA cache;
        public static ConfigIA Cargar()
        {
            if (cache == null) cache = Resources.Load<ConfigIA>("ConfigIA");
            if (cache == null) cache = CreateInstance<ConfigIA>();
            return cache;
        }
    }
}
