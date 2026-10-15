using UnityEngine;

namespace DataRoomXR
{
    /// <summary>Sonidos generados por código (no hace falta importar audios).</summary>
    public static class Sonidos
    {
        const int SR = 44100;

        static AudioClip Crear(string n, float dur, System.Func<float, float> f)
        {
            int len = Mathf.CeilToInt(SR * dur);
            var d = new float[len];
            for (int i = 0; i < len; i++) d[i] = Mathf.Clamp(f(i / (float)SR), -1f, 1f);
            var c = AudioClip.Create(n, len, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        static float Seno(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);

        public static AudioClip Listo() => Crear("Listo", 0.5f, t =>
            (Seno(t < 0.12f ? 660f : (t < 0.24f ? 880f : 1180f), t)) * Mathf.Exp(-t * 5f) * 0.45f);

        public static AudioClip Error() => Crear("Error", 0.5f, t =>
            (Seno(160f, t) > 0 ? 0.33f : -0.33f) * Mathf.Exp(-t * 4f));

        public static AudioClip Clic() => Crear("Clic", 0.12f, t =>
            Seno(520f, t) * Mathf.Exp(-t * 22f) * 0.4f);

        public static AudioClip Ambiente() => Crear("Ambiente", 4f, t =>
            (Seno(55f, t) * 0.35f + Seno(110f, t) * 0.15f + Seno(165f, t) * 0.08f));
    }
}
