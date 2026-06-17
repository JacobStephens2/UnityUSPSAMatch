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

        static AudioClip _gunshot, _paperHit, _steelDing, _buzzer, _music, _musicBlues, _hurt, _enemyDown;

        public static AudioClip Gunshot  => _gunshot  != null ? _gunshot  : (_gunshot  = MakeGunshot());
        public static AudioClip PaperHit => _paperHit != null ? _paperHit : (_paperHit = MakePaperHit());
        public static AudioClip SteelDing => _steelDing != null ? _steelDing : (_steelDing = MakeSteelDing());
        public static AudioClip Buzzer   => _buzzer   != null ? _buzzer   : (_buzzer   = MakeBuzzer());
        public static AudioClip MusicWestern => _music      != null ? _music      : (_music      = MakeMusic());
        public static AudioClip MusicBlues   => _musicBlues != null ? _musicBlues : (_musicBlues = MakeBlues());
        public static AudioClip Music    => MusicWestern; // back-compat alias
        public static AudioClip Hurt     => _hurt     != null ? _hurt     : (_hurt     = MakeHurt());
        public static AudioClip EnemyDown => _enemyDown != null ? _enemyDown : (_enemyDown = MakeEnemyDown());

        /// <summary>A coin-flip between the spaghetti-western theme and the blues boogie.</summary>
        public static AudioClip RandomMusic => UnityEngine.Random.value < 0.5f ? MusicWestern : MusicBlues;

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

        // Player hit: a low impact thump with a quick downward pitch sweep.
        static AudioClip MakeHurt()
        {
            float dur = 0.28f;
            int n = (int)(dur * Rate);
            var d = new float[n];
            var rng = new System.Random(13);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float freq = Mathf.Lerp(180f, 70f, Mathf.Clamp01(t / 0.18f));
                float body = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 14f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 40f);
                d[i] = Mathf.Clamp(body * 0.8f + noise * 0.4f, -1f, 1f);
            }
            return Clip("hurt", d);
        }

        // Outlaw down: a body-fall thud plus a short low groan.
        static AudioClip MakeEnemyDown()
        {
            float dur = 0.5f;
            int n = (int)(dur * Rate);
            var d = new float[n];
            var rng = new System.Random(23);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float groan = (Mathf.Sin(2f * Mathf.PI * 138f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 092f * t))
                              * Mathf.Exp(-t * 6f);
                float thudFreq = Mathf.Lerp(90f, 45f, Mathf.Clamp01(t / 0.1f));
                float thud = Mathf.Sin(2f * Mathf.PI * thudFreq * t) * Mathf.Exp(-t * 18f);
                float dust = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 9f) * 0.15f;
                d[i] = Mathf.Clamp(groan * 0.4f + thud * 0.7f + dust, -1f, 1f);
            }
            return Clip("enemydown", d);
        }

        // Background music: a spaghetti-western theme (no 12-bar blues). An
        // Andalusian cadence (Am–G–F–E — the classic Spanish/Western descent)
        // under a lonesome, vibrato lead; nylon-guitar arpeggio, a galloping
        // bass, a tremolo shimmer, and sparse percussion. Loops seamlessly.
        static AudioClip MakeMusic()
        {
            float bpm = 100f;
            float beat = 60f / bpm;
            int bars = 8;
            float dur = beat * 4f * bars;
            int n = (int)(dur * Rate);

            var lead = new float[n];
            var harm = new float[n];
            var bass = new float[n];
            var perc = new float[n];
            var rng = new System.Random(99);

            // Am G F E, repeated. Bass roots and chord tones (an octave up).
            float[] roots = { 110.00f, 98.00f, 87.31f, 82.41f }; // A2 G2 F2 E2
            float[][] chord =
            {
                new[] { 220.00f, 261.63f, 329.63f }, // Am: A3 C4 E4
                new[] { 196.00f, 246.94f, 293.66f }, // G : G3 B3 D4
                new[] { 174.61f, 220.00f, 261.63f }, // F : F3 A3 C4
                new[] { 164.81f, 207.65f, 246.94f }, // E : E3 G#3 B3
            };
            int[] arp = { 0, 1, 2, 1, 0, 1, 2, 1 };

            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = bar * 4f * beat;
                float root = roots[bar % 4];
                var ch = chord[bar % 4];

                float fifth = root * 1.5f;
                for (int b = 0; b < 4; b++)
                {
                    AddBassNote(bass, t0 + b * beat, beat * 0.46f, root, 0.9f);          // beat: root
                    AddBassNote(bass, t0 + (b + 0.5f) * beat, beat * 0.34f, fifth, 0.5f); // 'and': fifth
                }
                for (int e = 0; e < 8; e++)
                    AddPluck(harm, t0 + e * (beat * 0.5f), beat * 0.45f, ch[arp[e]], 0.30f);

                AddTremPad(harm, t0, 4f * beat, ch[0], 0.10f);
                AddTremPad(harm, t0, 4f * beat, ch[0] * 1.5f, 0.07f);

                AddKick(perc, t0);
                AddHat(perc, t0 + 1f * beat, rng);
                AddHat(perc, t0 + 2f * beat, rng);
                AddHat(perc, t0 + 3f * beat, rng);
            }

            // Lonesome lead (startBeat, durBeats, freq) over the cadence, A harmonic
            // minor for the Spanish colour (G# over the E chord).
            float[][] mel =
            {
                new[]{ 0f,1.5f,659.25f}, new[]{ 1.5f,0.5f,587.33f}, new[]{ 2f,2f,523.25f},     // Am: E5 D5 C5
                new[]{ 4f,1.5f,587.33f}, new[]{ 5.5f,0.5f,523.25f}, new[]{ 6f,2f,493.88f},     // G : D5 C5 B4
                new[]{ 8f,1.5f,523.25f}, new[]{ 9.5f,0.5f,440.00f}, new[]{10f,1f,523.25f}, new[]{11f,1f,440.00f}, // F: C5 A4 C5 A4
                new[]{12f,2f,493.88f},   new[]{14f,1f,415.30f},     new[]{15f,1f,329.63f},     // E : B4 G#4 E4
                new[]{16f,1f,880.00f},   new[]{17f,1f,783.99f},     new[]{18f,2f,659.25f},     // Am: A5 G5 E5
                new[]{20f,1f,783.99f},   new[]{21f,1f,698.46f},     new[]{22f,2f,587.33f},     // G : G5 F5 D5
                new[]{24f,1.5f,698.46f}, new[]{25.5f,0.5f,659.25f}, new[]{26f,2f,523.25f},     // F : F5 E5 C5
                new[]{28f,2f,493.88f},   new[]{30f,2f,440.00f},                                // E : B4 A4
            };
            foreach (var m in mel) AddLead(lead, m[0] * beat, m[1] * beat * 0.95f, m[2], 0.5f);

            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float l = (float)System.Math.Tanh(lead[i] * 1.3f);
                float b = (float)System.Math.Tanh(bass[i] * 1.6f);
                float mix = l * 0.5f + harm[i] * 0.5f + b * 0.6f + perc[i] * 0.45f;
                d[i] = (float)System.Math.Tanh(mix * 1.1f);
            }

            float peak = 0.0001f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            float norm = 0.92f / peak;
            for (int i = 0; i < n; i++) d[i] *= norm;

            return Clip("music", d);
        }

        // Twangy, vibrato lead (reedy via odd harmonics) — the lonesome whistle/guitar.
        static void AddLead(float[] buf, float startSec, float durSec, float freq, float amp)
        {
            int s = (int)(startSec * Rate);
            int len = (int)(durSec * Rate);
            float atk = 0.02f, rel = durSec * 0.25f;
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= buf.Length) break;
                float t = (float)i / Rate;
                // Gentle, slow vibrato that eases in (no fast warble at the attack).
                float vibDepth = 0.005f * Mathf.Clamp01(t / 0.25f);
                float vib = 1f + vibDepth * Mathf.Sin(2f * Mathf.PI * 4.6f * t);
                float ph = 2f * Mathf.PI * freq * vib * t;
                // Warmer/rounder tone (octave + soft fifth-ish), less reedy.
                float w = (Mathf.Sin(ph) + 0.18f * Mathf.Sin(2f * ph) + 0.06f * Mathf.Sin(3f * ph)) / 1.24f;
                float env = t < atk ? t / atk : (t > durSec - rel ? Mathf.Max(0f, (durSec - t) / rel) : 1f);
                buf[idx] += w * amp * env;
            }
        }

        // Nylon-guitar pluck (quick decay, a couple of harmonics).
        static void AddPluck(float[] buf, float startSec, float durSec, float freq, float amp)
        {
            int s = (int)(startSec * Rate);
            int len = (int)(durSec * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= buf.Length) break;
                float t = (float)i / Rate;
                float env = Mathf.Exp(-t * 5.5f) * (t < 0.005f ? t / 0.005f : 1f);
                float ph = 2f * Mathf.PI * freq * t;
                float w = Mathf.Sin(ph) + 0.4f * Mathf.Sin(2f * ph) + 0.15f * Mathf.Sin(3f * ph);
                buf[idx] += w * amp * env * 0.6f;
            }
        }

        // Warm gallop bass.
        static void AddBassNote(float[] buf, float startSec, float durSec, float freq, float amp)
        {
            int s = (int)(startSec * Rate);
            int len = (int)(durSec * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= buf.Length) break;
                float t = (float)i / Rate;
                float env = Mathf.Exp(-t * 3.5f) * (t < 0.004f ? t / 0.004f : 1f);
                float ph = 2f * Mathf.PI * freq * t;
                buf[idx] += (Mathf.Sin(ph) + 0.3f * Mathf.Sin(2f * ph)) * amp * env;
            }
        }

        // Sustained tremolo shimmer (amplitude wobble) under the chord.
        static void AddTremPad(float[] buf, float startSec, float durSec, float freq, float amp)
        {
            int s = (int)(startSec * Rate);
            int len = (int)(durSec * Rate);
            float atk = 0.05f, rel = 0.1f;
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= buf.Length) break;
                float t = (float)i / Rate;
                // Subtle, slow swell instead of a fast wobble.
                float trem = 0.88f + 0.12f * Mathf.Sin(2f * Mathf.PI * 2.2f * t);
                float env = t < atk ? t / atk : (t > durSec - rel ? Mathf.Max(0f, (durSec - t) / rel) : 1f);
                buf[idx] += Mathf.Sin(2f * Mathf.PI * freq * t) * amp * env * trem;
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

        // Alternate background music: a driving 12-bar blues-rock boogie —
        // distorted power-chord guitar (5-6 shuffle), overdriven bass on
        // straight 8ths, and a synthesized kick/snare/hat kit. Loops seamlessly.
        static AudioClip MakeBlues()
        {
            float bpm = 128f;
            float beat = 60f / bpm;
            int bars = 12;
            float dur = beat * bars * 4;
            int n = (int)(dur * Rate);

            var gtr = new float[n];
            var bass = new float[n];
            var drum = new float[n];
            var rng = new System.Random(99);

            float E = 82.41f, A = 110.00f, B = 123.47f;
            float[] barRoot = { E, E, E, E, A, A, E, E, B, A, E, B };
            float half = beat / 2f;

            for (int bar = 0; bar < bars; bar++)
            {
                float bassRoot = barRoot[bar];
                float gRoot = bassRoot * 2f;
                float barStart = bar * 4f * beat;

                for (int e = 0; e < 8; e++)
                {
                    float start = barStart + e * half;
                    int beatIdx = e / 2;
                    float upper = gRoot * Semi((beatIdx % 2 == 0) ? 7 : 9);
                    AddTone(gtr, start, half * 0.55f, gRoot, 0.5f, 1);
                    AddTone(gtr, start, half * 0.55f, upper, 0.42f, 1);
                    AddTone(bass, start, half * 0.6f, bassRoot, 0.9f, 2);
                }

                AddKick(drum, barStart + 0 * beat);
                AddKick(drum, barStart + 2 * beat);
                AddKick(drum, barStart + 2.5f * beat);
                AddSnare(drum, barStart + 1 * beat, rng);
                AddSnare(drum, barStart + 3 * beat, rng);
                for (int e = 0; e < 8; e++) AddHat(drum, barStart + e * half, rng);
            }

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

            return Clip("music_blues", d);
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
    }
}
