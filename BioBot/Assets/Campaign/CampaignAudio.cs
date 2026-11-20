using System;
using UnityEngine;

namespace BioBot
{
    public enum SfxId
    {
        Plant, Shoot, Hit, ArmorCrack, CollectOrb, PushBottle, RescueFish,
        Dive, Surface, Pulse, Turbine, WaveHorn, Click, Coin, Win, Lose
    }

    public sealed class CampaignAudio
    {
        private const int SampleRate = 22050;
        private readonly AudioSource sfxSource;
        private readonly AudioSource bgmSource;
        private readonly AudioClip[] clips;
        private bool muted;

        public bool Muted
        {
            get => muted;
            set
            {
                muted = value;
                if (bgmSource != null) bgmSource.mute = value;
                if (sfxSource != null) sfxSource.mute = value;
            }
        }

        public CampaignAudio(GameObject host, bool startMuted)
        {
            if (host.GetComponent<AudioListener>() == null && UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
                host.AddComponent<AudioListener>();

            bgmSource = host.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.volume = 0.18f;
            bgmSource.playOnAwake = false;

            sfxSource = host.AddComponent<AudioSource>();
            sfxSource.volume = 0.45f;
            sfxSource.playOnAwake = false;

            clips = new AudioClip[Enum.GetValues(typeof(SfxId)).Length];
            BuildClips();
            bgmSource.clip = BuildBgm();
            Muted = startMuted;
            bgmSource.Play();
        }

        public void Play(SfxId id, float volume = 1f)
        {
            if (muted || sfxSource == null) return;
            int idx = (int)id;
            if (idx >= 0 && idx < clips.Length && clips[idx] != null)
                sfxSource.PlayOneShot(clips[idx], volume);
        }

        private void BuildClips()
        {
            clips[(int)SfxId.Plant] = MakeSweep("Plant", 0.14f, 260f, 580f, 0.55f, false);
            clips[(int)SfxId.Shoot] = MakeSweep("Shoot", 0.09f, 620f, 340f, 0.32f, false);
            clips[(int)SfxId.Hit] = MakeNoiseTone("Hit", 0.08f, 220f, 0.35f, 0.35f);
            clips[(int)SfxId.ArmorCrack] = MakeNoiseTone("ArmorCrack", 0.18f, 520f, 0.6f, 0.45f);
            clips[(int)SfxId.CollectOrb] = MakeArp("Orb", new[] { 587.3f, 880f }, 0.07f, 0.45f);
            clips[(int)SfxId.PushBottle] = MakeSweep("Push", 0.14f, 180f, 310f, 0.4f, true);
            clips[(int)SfxId.RescueFish] = MakeArp("Rescue", new[] { 523.2f, 659.2f, 784f, 1046.5f }, 0.075f, 0.5f);
            clips[(int)SfxId.Dive] = MakeSweep("Dive", 0.28f, 520f, 130f, 0.5f, true);
            clips[(int)SfxId.Surface] = MakeSweep("Surface", 0.28f, 150f, 600f, 0.5f, true);
            clips[(int)SfxId.Pulse] = MakeNoiseTone("Pulse", 0.26f, 140f, 0.35f, 0.55f);
            clips[(int)SfxId.Turbine] = MakeNoiseTone("Turbine", 0.45f, 110f, 0.55f, 0.6f);
            clips[(int)SfxId.WaveHorn] = MakeArp("Wave", new[] { 220f, 277.2f, 330f }, 0.16f, 0.48f);
            clips[(int)SfxId.Click] = MakeSweep("Click", 0.045f, 760f, 920f, 0.25f, false);
            clips[(int)SfxId.Coin] = MakeArp("Coin", new[] { 987.8f, 1318.5f }, 0.065f, 0.42f);
            clips[(int)SfxId.Win] = MakeArp("Win", new[] { 523.2f, 659.2f, 784f, 1046.5f, 1318.5f }, 0.11f, 0.52f);
            clips[(int)SfxId.Lose] = MakeArp("Lose", new[] { 392f, 349.2f, 311.1f, 261.6f }, 0.15f, 0.5f);
        }

        private static AudioClip MakeSweep(string name, float dur, float f0, float f1, float amp, bool noisy)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(dur * SampleRate));
            var data = new float[n];
            float phase = 0f;
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Sin(t * Mathf.PI) * (1f - t * 0.4f);
                float freq = Mathf.Lerp(f0, f1, t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float sample = Mathf.Sin(phase);
                if (noisy) sample = sample * 0.65f + ((float)rng.NextDouble() * 2f - 1f) * 0.35f;
                data[i] = sample * env * amp;
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeNoiseTone(string name, float dur, float freq, float noiseMix, float amp)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(dur * SampleRate));
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode() + 17);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Pow(1f - t, 1.6f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * (1f - t * 0.35f) * i / SampleRate);
                float noise = (float)rng.NextDouble() * 2f - 1f;
                data[i] = (tone * (1f - noiseMix) + noise * noiseMix) * env * amp;
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeArp(string name, float[] notes, float noteDur, float amp)
        {
            int perNote = Mathf.RoundToInt(noteDur * SampleRate);
            int total = perNote * notes.Length;
            var data = new float[total];
            for (int k = 0; k < notes.Length; k++)
            {
                float f = notes[k];
                for (int i = 0; i < perNote; i++)
                {
                    float t = i / (float)perNote;
                    float env = Mathf.Min(1f, t * 14f) * Mathf.Pow(1f - t, 1.1f);
                    float s = Mathf.Sin(2f * Mathf.PI * f * i / SampleRate) * 0.75f
                            + Mathf.Sin(4f * Mathf.PI * f * i / SampleRate) * 0.25f;
                    data[k * perNote + i] = s * env * amp;
                }
            }
            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildBgm()
        {
            // Cheerful, relaxing PvZ-inspired aquatic pentatonic marimba loop (8 seconds)
            float[] melody = {
                261.6f, 329.6f, 392.0f, 523.2f, 392.0f, 329.6f, 293.7f, 392.0f,
                329.6f, 392.0f, 440.0f, 523.2f, 440.0f, 392.0f, 329.6f, 261.6f,
                293.7f, 349.2f, 440.0f, 587.3f, 440.0f, 349.2f, 329.6f, 392.0f,
                261.6f, 329.6f, 392.0f, 440.0f, 392.0f, 329.6f, 293.7f, 261.6f
            };
            float stepDur = 0.25f;
            int stepSamples = Mathf.RoundToInt(stepDur * SampleRate);
            int total = stepSamples * melody.Length;
            var data = new float[total];
            for (int s = 0; s < melody.Length; s++)
            {
                float f = melody[s];
                float bass = (s % 4 == 0) ? f * 0.5f : 0f;
                for (int i = 0; i < stepSamples; i++)
                {
                    float t = i / (float)stepSamples;
                    float env = Mathf.Min(1f, t * 24f) * Mathf.Pow(1f - t, 2.2f);
                    float marimba = Mathf.Sin(2f * Mathf.PI * f * i / SampleRate) * 0.65f
                                  + Mathf.Sin(6f * Mathf.PI * f * i / SampleRate) * 0.12f;
                    float sub = bass > 0f ? Mathf.Sin(2f * Mathf.PI * bass * i / SampleRate) * 0.45f * (1f - t) : 0f;
                    data[s * stepSamples + i] = (marimba * env + sub) * 0.38f;
                }
            }
            var clip = AudioClip.Create("BioBotBGM", total, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
