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

        // Background music: a driving 12-bar blues-rock boogie — distorted
        // power-chord guitar (5-6 shuffle), overdriven bass on straight 8ths,
        // and a synthesized kick/snare/hat kit. Loops seamlessly.
        static AudioClip MakeMusic()
        {
            float bpm = 128f;
            float beat = 60f / bpm;
            int bars = 12;
            int beatsTotal = bars * 4;
            float dur = beat * beatsTotal;
            int n = (int)(dur * Rate);

            var gtr = new float[n];   // guitar (distorted)
            var bass = new float[n];  // bass (overdriven)
            var drum = new float[n];  // drums (clean punch)
            var rng = new System.Random(99);

            // 12-bar blues in E: I=E I=E I=E I=E IV=A IV=A I=E I=E V=B IV=A I=E V=B
            float E = 82.41f, A = 110.00f, B = 123.47f;
            float[] barRoot = { E, E, E, E, A, A, E, E, B, A, E, B };
            float half = beat / 2f;

            for (int bar = 0; bar < bars; bar++)
            {
                float bassRoot = barRoot[bar];
                float gRoot = bassRoot * 2f;       // guitar an octave up
                float barStart = bar * 4f * beat;

                for (int e = 0; e < 8; e++)         // eight 8th-notes per bar
                {
                    float start = barStart + e * half;
                    // 5-6 boogie: 5th (7 st) on beats 1&3, 6th (9 st) on 2&4.
                    int beatIdx = e / 2;
                    float upper = gRoot * Semi((beatIdx % 2 == 0) ? 7 : 9);

                    // Palm-muted power-chord stab (root + upper), sawtooth for grit.
                    AddTone(gtr, start, half * 0.55f, gRoot, 0.5f, 1);
                    AddTone(gtr, start, half * 0.55f, upper, 0.42f, 1);

                    // Bass drives straight 8ths on the root, square for punch.
                    AddTone(bass, start, half * 0.6f, bassRoot, 0.9f, 2);
                }

                // Drums: kick on 1 & 3 (+ '&' of 3), snare backbeat on 2 & 4, hats on 8ths.
                AddKick(drum, barStart + 0 * beat);
                AddKick(drum, barStart + 2 * beat);
                AddKick(drum, barStart + 2.5f * beat);
                AddSnare(drum, barStart + 1 * beat, rng);
                AddSnare(drum, barStart + 3 * beat, rng);
                for (int e = 0; e < 8; e++) AddHat(drum, barStart + e * half, rng);
            }

            // Waveshape distortion (guitar hot, bass milder), then mix + limit.
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float g = (float)System.Math.Tanh(gtr[i] * 4.5f);
                float b = (float)System.Math.Tanh(bass[i] * 2.2f);
                float mix = g * 0.42f + b * 0.55f + drum[i] * 0.9f;
                d[i] = (float)System.Math.Tanh(mix * 1.15f);
            }

            float peak = 0.0001f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            float norm = 0.92f / peak;
            for (int i = 0; i < n; i++) d[i] *= norm;

            return Clip("music", d);
        }

        static float Semi(int semitones) => Mathf.Pow(2f, semitones / 12f);

        // wave: 0 sine, 1 saw, 2 square.
        static void AddTone(float[] buf, float startSec, float durSec, float freq, float amp, int wave)
        {
            int s = (int)(startSec * Rate);
            int len = (int)(durSec * Rate);
            float rel = durSec * 0.3f, atk = 0.004f;
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= buf.Length) break;
                float t = (float)i / Rate;
                float cyc = freq * t;
                float w = wave == 1 ? 2f * (cyc - Mathf.Floor(cyc)) - 1f
                        : wave == 2 ? (Mathf.Sin(cyc * 2f * Mathf.PI) >= 0f ? 1f : -1f)
                        : Mathf.Sin(cyc * 2f * Mathf.PI);
                float env = t < atk ? t / atk : (t > durSec - rel ? Mathf.Max(0f, (durSec - t) / rel) : 1f);
                buf[idx] += w * amp * env;
            }
        }

        static void AddKick(float[] buf, float startSec)
        {
            int s = (int)(startSec * Rate);
            int len = (int)(0.18f * Rate);
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= buf.Length) break;
                float t = (float)i / Rate;
                float freq = Mathf.Lerp(120f, 48f, Mathf.Clamp01(t / 0.05f));
                phase += freq / Rate;
                float body = Mathf.Sin(phase * 2f * Mathf.PI) * Mathf.Exp(-t * 30f);
                float click = i < 4 ? 0.5f : 0f;
                buf[idx] += body * 1.0f + click;
            }
        }

        static void AddSnare(float[] buf, float startSec, System.Random rng)
        {
            int s = (int)(startSec * Rate);
            int len = (int)(0.16f * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= buf.Length) break;
                float t = (float)i / Rate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float tone = Mathf.Sin(2f * Mathf.PI * 190f * t);
                buf[idx] += noise * 0.6f * Mathf.Exp(-t * 30f) + tone * 0.3f * Mathf.Exp(-t * 45f);
            }
        }

        static void AddHat(float[] buf, float startSec, System.Random rng)
        {
            int s = (int)(startSec * Rate);
            int len = (int)(0.04f * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= buf.Length) break;
                float t = (float)i / Rate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                buf[idx] += noise * 0.22f * Mathf.Exp(-t * 130f);
            }
        }
    }
}
