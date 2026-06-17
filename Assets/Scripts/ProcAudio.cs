using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// Generates every sound in the game at runtime with simple DSP, so the
    /// project carries no audio files. Clips are built once and cached.
    /// </summary>
    public static class ProcAudio
    {
        const int Rate = 44100;

        static AudioClip _gunshot, _paperHit, _steelDing, _buzzer, _music;

        public static AudioClip Gunshot  => _gunshot  != null ? _gunshot  : (_gunshot  = MakeGunshot());
        public static AudioClip PaperHit => _paperHit != null ? _paperHit : (_paperHit = MakePaperHit());
        public static AudioClip SteelDing => _steelDing != null ? _steelDing : (_steelDing = MakeSteelDing());
        public static AudioClip Buzzer   => _buzzer   != null ? _buzzer   : (_buzzer   = MakeBuzzer());
        public static AudioClip Music    => _music    != null ? _music    : (_music    = MakeMusic());

        static AudioClip Clip(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        // Gunshot: noise crack + a low body thump + a high transient.
        static AudioClip MakeGunshot()
        {
            float dur = 0.20f;
            int n = (int)(dur * Rate);
            var d = new float[n];
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float body = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 22f);
                float crack = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 120f);
                d[i] = Mathf.Clamp(noise * 0.85f * Mathf.Exp(-t * 42f) + body * 0.5f + crack * 0.4f, -1f, 1f);
            }
            return Clip("gunshot", d);
        }

        // Paper impact: short muted thwack.
        static AudioClip MakePaperHit()
        {
            float dur = 0.07f;
            int n = (int)(dur * Rate);
            var d = new float[n];
            var rng = new System.Random(3);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Exp(-t * 70f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                d[i] = (noise * 0.5f + Mathf.Sin(2f * Mathf.PI * 230f * t) * 0.4f) * env;
            }
            return Clip("paperhit", d);
        }

        // Steel: metallic ring with a few partials.
        static AudioClip MakeSteelDing()
        {
            float dur = 0.45f;
            int n = (int)(dur * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float e = Mathf.Exp(-t * 7f);
                d[i] = (Mathf.Sin(2f * Mathf.PI * 1570f * t) * 0.55f
                      + Mathf.Sin(2f * Mathf.PI * 2350f * t) * 0.30f
                      + Mathf.Sin(2f * Mathf.PI * 3300f * t) * 0.18f) * e * 0.8f;
            }
            return Clip("steelding", d);
        }

        // Start buzzer: a square-wave beep.
        static AudioClip MakeBuzzer()
        {
            float dur = 0.4f;
            int n = (int)(dur * Rate);
            var d = new float[n];
            float atk = 0.005f, rel = 0.03f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = t < atk ? t / atk : (t > dur - rel ? Mathf.Max(0f, (dur - t) / rel) : 1f);
                float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 800f * t));
                d[i] = sq * 0.32f * env;
            }
            return Clip("buzzer", d);
        }

        // Background music: a short looping A-minor arpeggio + bass pulse.
        static AudioClip MakeMusic()
        {
            float bpm = 96f;
            float beat = 60f / bpm;
            int beats = 16;
            float dur = beat * beats;
            int n = (int)(dur * Rate);
            var d = new float[n];

            float step = beat / 2f;
            int eighths = beats * 2;
            float[] arp = { 220f, 261.63f, 329.63f, 261.63f, 246.94f, 329.63f, 392f, 329.63f };
            for (int k = 0; k < eighths; k++)
                AddNote(d, k * step, step * 0.9f, arp[k % arp.Length], 0.16f);

            float[] bass = { 110f, 110f, 87.31f, 82.41f }; // A A F E
            for (int b = 0; b < beats; b++)
                AddNote(d, b * beat, beat * 0.92f, bass[b % bass.Length], 0.22f);

            // Gentle peak normalise.
            float peak = 0.0001f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            float g = Mathf.Min(1f, 0.9f / peak);
            for (int i = 0; i < n; i++) d[i] *= g;

            return Clip("music", d);
        }

        static void AddNote(float[] buf, float startSec, float durSec, float freq, float amp)
        {
            int s = (int)(startSec * Rate);
            int len = (int)(durSec * Rate);
            float rel = durSec * 0.35f, atk = 0.008f;
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= buf.Length) break;
                float t = (float)i / Rate;
                float env = t < atk ? t / atk : (t > durSec - rel ? Mathf.Max(0f, (durSec - t) / rel) : 1f);
                buf[idx] += Mathf.Sin(2f * Mathf.PI * freq * t) * amp * env;
            }
        }
    }
}
