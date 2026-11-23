using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Sintetiza por código todos los sonidos del juego (efectos estilo chiptune y música ambiental en bucle)
    /// y los guarda como .wav en Assets/Resources/Audio, donde los encuentra AudioManager.
    /// Usa semillas fijas: generar otra vez da exactamente los mismos archivos.
    /// Menú: Eternal Sleep > Generar sonidos.
    /// </summary>
    public static class AudioArt
    {
        private const string Folder = "Assets/Resources/Audio";
        private const int Rate = 22050;

        [MenuItem("Eternal Sleep/Generar sonidos")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            var sounds = new Dictionary<string, float[]>
            {
                ["salto"] = Jump(),
                ["doble_salto"] = DoubleJump(),
                ["impulso"] = Dash(),
                ["golpe_suelo"] = Slam(),
                ["ataque"] = Swing(0.11f, 1f),
                ["ataque_cargado"] = Swing(0.22f, 0.6f),
                ["carga_lista"] = Chime(new[] { 88, 95 }, 0.09f, 0.5f),
                ["impacto_enemigo"] = EnemyHit(),
                ["muerte_enemigo"] = EnemyDeath(),
                ["dano"] = Hurt(),
                ["muerte"] = Death(),
                ["lagrima"] = Chime(new[] { 84, 88, 91, 96 }, 0.07f, 0.6f),
                ["mejora"] = Chime(new[] { 69, 72, 76, 81, 84, 88 }, 0.11f, 1.1f),
                ["corazon"] = Chime(new[] { 72, 79 }, 0.12f, 0.6f),
                ["dialogo_letra"] = Blip(620f, 0.035f, Wave.Square, 0.35f),
                ["menu_mover"] = Blip(740f, 0.05f, Wave.Triangle, 0.5f),
                ["menu_aceptar"] = Chime(new[] { 76, 83 }, 0.06f, 0.25f),
                ["puerta"] = Door(),
                ["portal"] = Portal(),
                ["rugido_jefe"] = Roar(),
                ["musica_menu"] = Music(Tracks.Menu),
                ["musica_pueblo"] = Music(Tracks.Village),
                ["musica_sueno"] = Music(Tracks.Dream),
                ["musica_jefe"] = Music(Tracks.Boss),
            };

            try
            {
                int i = 0;
                foreach (var pair in sounds)
                {
                    EditorUtility.DisplayProgressBar("Eternal Sleep", "Guardando " + pair.Key, i++ / (float)sounds.Count);
                    WriteWav(Path.Combine(Folder, pair.Key + ".wav"), pair.Value);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            foreach (var name in sounds.Keys) ConfigureImport(Folder + "/" + name + ".wav", name.StartsWith("musica_"));
            Debug.Log($"[Eternal Sleep] {sounds.Count} sonidos generados en {Folder}.");
        }

        private static void ConfigureImport(string path, bool music)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;
            importer.forceToMono = true;
            importer.loadInBackground = music;
            var s = importer.defaultSampleSettings;
            s.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            s.quality = 0.6f;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }

        // ---------- Piezas básicas ----------

        private enum Wave { Square, Triangle, Saw, Sine }

        private static float Osc(Wave w, double phase)
        {
            double p = phase - Math.Floor(phase);
            switch (w)
            {
                case Wave.Square: return p < 0.5 ? 1f : -1f;
                case Wave.Triangle: return (float)(p < 0.5 ? 4 * p - 1 : 3 - 4 * p);
                case Wave.Saw: return (float)(2 * p - 1);
                default: return (float)Math.Sin(p * 2 * Math.PI);
            }
        }

        private static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        private static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

        /// <summary>Tono que se desliza de f0 a f1 con caída de volumen (la base de casi todos los efectos).</summary>
        private static void Tone(float[] buf, int start, float seconds, Wave w, float f0, float f1, float vol,
                                 float attack = 0.005f, float vibrato = 0f, float curve = 2f)
        {
            int n = Mathf.Min(Mathf.CeilToInt(seconds * Rate), buf.Length - start);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t) * (1f + vibrato * Mathf.Sin(i / (float)Rate * 2f * Mathf.PI * 9f));
                phase += f / Rate;
                float env = Mathf.Min(1f, i / (attack * Rate + 1f)) * Mathf.Pow(1f - t, curve);
                buf[start + i] += Osc(w, phase) * env * vol;
            }
        }

        /// <summary>Ruido con filtro paso bajo que se mueve de c0 a c1 (0..1, más alto = más brillante).</summary>
        private static void Noise(float[] buf, int start, float seconds, float c0, float c1, float vol, int seed, float curve = 2f)
        {
            var rng = new System.Random(seed);
            int n = Mathf.Min(Mathf.CeilToInt(seconds * Rate), buf.Length - start);
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float x = (float)(rng.NextDouble() * 2 - 1);
                y += Mathf.Lerp(c0, c1, t) * (x - y);
                buf[start + i] += y * Mathf.Pow(1f - t, curve) * vol;
            }
        }

        private static float[] Blip(float freq, float seconds, Wave w, float vol)
        {
            var b = Buffer(seconds);
            Tone(b, 0, seconds, w, freq, freq * 0.97f, vol, 0.002f, 0f, 1.5f);
            return b;
        }

        /// <summary>Notas de campanita una detrás de otra (recoger, mejoras, menús).</summary>
        private static float[] Chime(int[] notes, float step, float tail)
        {
            var b = Buffer(step * notes.Length + tail);
            for (int i = 0; i < notes.Length; i++)
            {
                float f = Midi(notes[i]);
                int at = Mathf.RoundToInt(i * step * Rate);
                Tone(b, at, tail, Wave.Sine, f, f, 0.35f, 0.003f, 0f, 3f);
                Tone(b, at, tail * 0.6f, Wave.Triangle, f * 2f, f * 2f, 0.08f, 0.003f, 0f, 4f);
            }
            return b;
        }

        // ---------- Efectos ----------

        private static float[] Jump()
        {
            var b = Buffer(0.16f);
            Tone(b, 0, 0.16f, Wave.Square, 260f, 620f, 0.22f, 0.003f, 0f, 1.6f);
            return b;
        }

        private static float[] DoubleJump()
        {
            var b = Buffer(0.24f);
            Tone(b, 0, 0.2f, Wave.Triangle, 480f, 980f, 0.35f);
            Noise(b, 0, 0.24f, 0.6f, 0.2f, 0.12f, 11); // aleteo de polvo
            return b;
        }

        private static float[] Dash()
        {
            var b = Buffer(0.26f);
            Noise(b, 0, 0.26f, 0.08f, 0.6f, 0.6f, 21, 1.4f);
            Tone(b, 0, 0.18f, Wave.Sine, 220f, 110f, 0.2f);
            return b;
        }

        private static float[] Slam()
        {
            var b = Buffer(0.45f);
            Tone(b, 0, 0.45f, Wave.Sine, 130f, 38f, 0.7f, 0.002f, 0f, 1.5f);
            Noise(b, 0, 0.3f, 0.5f, 0.05f, 0.5f, 31);
            return b;
        }

        private static float[] Swing(float seconds, float bright)
        {
            var b = Buffer(seconds);
            Noise(b, 0, seconds, 0.15f * bright, 0.7f * bright, 0.45f, 41, 1.8f);
            Tone(b, 0, seconds, Wave.Triangle, 900f * bright, 300f * bright, 0.12f);
            return b;
        }

        private static float[] EnemyHit()
        {
            var b = Buffer(0.14f);
            Tone(b, 0, 0.14f, Wave.Square, 210f, 70f, 0.25f);
            Noise(b, 0, 0.08f, 0.7f, 0.3f, 0.35f, 51);
            return b;
        }

        private static float[] EnemyDeath()
        {
            var b = Buffer(0.5f);
            Tone(b, 0, 0.5f, Wave.Square, 420f, 55f, 0.2f, 0.003f, 0.05f, 1.4f);
            Noise(b, 0, 0.5f, 0.4f, 0.03f, 0.35f, 61, 1.5f);
            return b;
        }

        private static float[] Hurt()
        {
            var b = Buffer(0.28f);
            Tone(b, 0, 0.28f, Wave.Square, 240f, 110f, 0.25f, 0.002f, 0.08f, 1.5f);
            Noise(b, 0, 0.1f, 0.6f, 0.2f, 0.25f, 71);
            return b;
        }

        private static float[] Death()
        {
            var b = Buffer(1.3f);
            Tone(b, 0, 1.3f, Wave.Triangle, 440f, 52f, 0.45f, 0.01f, 0.03f, 1.2f);
            Tone(b, 0, 1.0f, Wave.Sine, 220f, 30f, 0.3f, 0.01f, 0f, 1.5f);
            return b;
        }

        private static float[] Door()
        {
            var b = Buffer(0.5f);
            Tone(b, 0, 0.45f, Wave.Saw, 95f, 70f, 0.15f, 0.04f, 0.12f, 1.2f); // crujido
            Noise(b, Mathf.RoundToInt(0.38f * Rate), 0.12f, 0.3f, 0.05f, 0.5f, 81); // golpe al cerrar
            return b;
        }

        private static float[] Portal()
        {
            var b = Buffer(0.9f);
            Tone(b, 0, 0.9f, Wave.Sine, 200f, 1200f, 0.3f, 0.2f, 0.04f, 1f);
            Tone(b, 0, 0.9f, Wave.Triangle, 300f, 1800f, 0.1f, 0.2f, 0.06f, 1f);
            return b;
        }

        private static float[] Roar()
        {
            var b = Buffer(1.1f);
            Noise(b, 0, 1.1f, 0.05f, 0.15f, 0.8f, 91, 1.2f);
            Tone(b, 0, 1.1f, Wave.Saw, 75f, 55f, 0.25f, 0.08f, 0.15f, 1.2f);
            return b;
        }

        // ---------- Música ----------

        private class Track
        {
            public int Bpm;
            public int[][] Chords;     // notas MIDI de cada acorde (un acorde por compás)
            public int Bars;           // compases en total (los acordes se repiten)
            public Wave Pad = Wave.Triangle;
            public float PadVol = 0.07f;
            public float Detune = 0.004f;
            public float Wobble;       // vaivén lento de la afinación (inquietud)
            public int ArpSteps = 8;   // notas de arpegio por compás
            public float ArpVol = 0.09f;
            public float ArpSkip = 0.25f;
            public bool Drums;
            public float Echo = 0.35f;
            public int Seed;
        }

        private static class Tracks
        {
            // La menor: Am F C E
            public static readonly Track Menu = new Track
            {
                Bpm = 72, Bars = 12, Seed = 1, ArpSteps = 8, ArpSkip = 0.3f,
                Chords = new[] { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 }, new[] { 52, 56, 59 } },
            };

            // Re menor: Dm Bb F A
            public static readonly Track Village = new Track
            {
                Bpm = 64, Bars = 10, Seed = 2, ArpSteps = 6, ArpSkip = 0.35f, Pad = Wave.Sine, PadVol = 0.09f,
                Chords = new[] { new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 53, 57, 60 }, new[] { 45, 49, 52 } },
            };

            // Do menor con notas que chocan: Cm Db Ab G
            public static readonly Track Dream = new Track
            {
                Bpm = 56, Bars = 9, Seed = 3, ArpSteps = 5, ArpSkip = 0.45f, Wobble = 0.012f, Detune = 0.009f, Echo = 0.5f,
                Chords = new[] { new[] { 48, 51, 55, 62 }, new[] { 49, 53, 56, 60 }, new[] { 44, 48, 51, 55 }, new[] { 43, 47, 50, 56 } },
            };

            // Mi menor rápido: Em C D B
            public static readonly Track Boss = new Track
            {
                Bpm = 132, Bars = 22, Seed = 4, ArpSteps = 16, ArpSkip = 0.1f, Pad = Wave.Saw, PadVol = 0.04f,
                ArpVol = 0.07f, Drums = true, Echo = 0.2f,
                Chords = new[] { new[] { 52, 55, 59 }, new[] { 48, 52, 55 }, new[] { 50, 54, 57 }, new[] { 47, 51, 54 } },
            };
        }

        private static float[] Music(Track track)
        {
            float bar = 4f * 60f / track.Bpm;
            var buf = Buffer(bar * track.Bars);
            int barLen = Mathf.RoundToInt(bar * Rate);
            var rng = new System.Random(track.Seed);

            for (int b = 0; b < track.Bars; b++)
            {
                var chord = track.Chords[b % track.Chords.Length];
                int start = b * barLen;
                Pad(buf, start, barLen, chord, track);
                Bass(buf, start, barLen, chord[0] - 12, track);

                int step = barLen / track.ArpSteps;
                for (int s = 0; s < track.ArpSteps; s++)
                {
                    if (rng.NextDouble() < track.ArpSkip) continue;
                    int note = chord[rng.Next(chord.Length)] + 12 * (rng.NextDouble() < 0.3 ? 2 : 1);
                    float f = Midi(note);
                    Tone(buf, start + s * step, step * 3f / Rate, Wave.Triangle, f, f, track.ArpVol, 0.004f, 0f, 3f);
                }
                if (track.Drums) Drums(buf, start, barLen, b);
            }

            ApplyEcho(buf, Mathf.RoundToInt(bar / 4f * 3f * Rate), track.Echo); // eco a 3 corcheas
            Normalize(buf, 0.8f);
            return buf;
        }

        /// <summary>Acorde sostenido con entrada y salida suaves y dos osciladores un poco desafinados por nota.</summary>
        private static void Pad(float[] buf, int start, int len, int[] chord, Track track)
        {
            foreach (int note in chord)
            {
                float f = Midi(note);
                double p1 = 0, p2 = 0;
                for (int i = 0; i < len && start + i < buf.Length; i++)
                {
                    float t = i / (float)len;
                    float env = Mathf.Min(1f, t / 0.25f) * Mathf.Min(1f, (1f - t) / 0.2f);
                    float w = 1f + track.Wobble * Mathf.Sin((start + i) / (float)Rate * 0.7f);
                    p1 += f * w * (1f + track.Detune) / Rate;
                    p2 += f * w * (1f - track.Detune) / Rate;
                    buf[start + i] += (Osc(track.Pad, p1) + Osc(track.Pad, p2)) * 0.5f * env * track.PadVol;
                }
            }
        }

        private static void Bass(float[] buf, int start, int len, int note, Track track)
        {
            float f = Midi(note);
            int pulses = track.Drums ? 8 : 2;
            int step = len / pulses;
            for (int i = 0; i < pulses; i++)
                Tone(buf, start + i * step, step / (float)Rate, Wave.Sine, f, f, track.Drums ? 0.22f : 0.16f, 0.01f, 0f, 1.2f);
        }

        private static void Drums(float[] buf, int start, int len, int bar)
        {
            int beat = len / 4;
            for (int i = 0; i < 4; i++)
            {
                int at = start + i * beat;
                if (i % 2 == 0) Tone(buf, at, 0.18f, Wave.Sine, 110f, 40f, 0.5f, 0.001f, 0f, 2f); // bombo
                else Noise(buf, at, 0.12f, 0.5f, 0.3f, 0.25f, 100 + bar * 4 + i);                 // caja
                Noise(buf, at + beat / 2, 0.04f, 0.9f, 0.9f, 0.08f, 200 + bar * 4 + i);          // platillo
            }
        }

        /// <summary>Eco circular: lo que sale por el final vuelve a entrar por el principio, así el bucle no se corta.</summary>
        private static void ApplyEcho(float[] buf, int delay, float feedback)
        {
            if (feedback <= 0f || delay <= 0) return;
            var dry = (float[])buf.Clone();
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < buf.Length; i++)
                    buf[i] = dry[i] + buf[(i - delay + buf.Length) % buf.Length] * feedback;
        }

        private static void Normalize(float[] buf, float peak)
        {
            float max = 0f;
            foreach (float s in buf) max = Mathf.Max(max, Mathf.Abs(s));
            if (max <= 0f) return;
            float k = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
        }

        // ---------- WAV ----------

        private static void WriteWav(string path, float[] samples)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);       // PCM
                w.Write((short)1);       // mono
                w.Write(Rate);
                w.Write(Rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (float s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767f));
            }
        }
    }
}
