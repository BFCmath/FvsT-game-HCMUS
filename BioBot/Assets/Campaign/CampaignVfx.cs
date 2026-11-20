using System.Collections.Generic;
using UnityEngine;

namespace BioBot
{
    public sealed class CampaignVfx
    {
        public struct Particle
        {
            public Layer Layer;
            public Vector2 Pos, Vel;
            public float Life, MaxLife, Size;
            public Color Color;
            public bool Ring;
        }

        public struct FloatingText
        {
            public Layer Layer;
            public Vector2 Pos;
            public string Text;
            public Color Color;
            public float Life, MaxLife;
        }

        public readonly List<Particle> Particles = new List<Particle>(128);
        public readonly List<FloatingText> Texts = new List<FloatingText>(32);

        public float ShakeTime { get; private set; }
        public float ShakeIntensity { get; private set; }
        public string WaveBannerText { get; private set; }
        public float WaveBannerTime { get; private set; }
        public string ToastTitle { get; private set; }
        public string ToastSub { get; private set; }
        public float ToastTime { get; private set; }

        public void Clear()
        {
            Particles.Clear();
            Texts.Clear();
            ShakeTime = 0f;
            WaveBannerTime = 0f;
        }

        public void TriggerShake(float duration, float intensity = 4f)
        {
            ShakeTime = Mathf.Max(ShakeTime, duration);
            ShakeIntensity = Mathf.Max(ShakeIntensity, intensity);
        }

        public void ShowWaveBanner(string text, float duration = 2.6f)
        {
            WaveBannerText = text;
            WaveBannerTime = duration;
        }

        public void ShowToast(string title, string subtitle, float duration = 3.6f)
        {
            ToastTitle = title;
            ToastSub = subtitle;
            ToastTime = duration;
        }

        public void SpawnBurst(Layer layer, Vector2 pixelPos, Color color, int count = 8, float speed = 70f, float size = 4f)
        {
            for (int i = 0; i < count; i++)
            {
                float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f);
                float spd = speed * Random.Range(0.55f, 1.25f);
                Particles.Add(new Particle
                {
                    Layer = layer,
                    Pos = pixelPos,
                    Vel = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd - 14f),
                    Life = Random.Range(0.24f, 0.52f),
                    MaxLife = 0.52f,
                    Size = size * Random.Range(0.7f, 1.3f),
                    Color = color
                });
            }
        }

        public void SpawnRing(Layer layer, Vector2 pixelPos, Color color, float maxSize = 46f)
        {
            Particles.Add(new Particle
            {
                Layer = layer,
                Pos = pixelPos,
                Vel = Vector2.zero,
                Life = 0.32f,
                MaxLife = 0.32f,
                Size = maxSize,
                Color = color,
                Ring = true
            });
        }

        public void SpawnText(Layer layer, Vector2 pixelPos, string text, Color color, float life = 1.05f)
        {
            Texts.Add(new FloatingText
            {
                Layer = layer,
                Pos = pixelPos,
                Text = text,
                Color = color,
                Life = life,
                MaxLife = life
            });
        }

        public void Tick(float delta)
        {
            if (ShakeTime > 0f) ShakeTime = Mathf.Max(0f, ShakeTime - delta);
            if (WaveBannerTime > 0f) WaveBannerTime = Mathf.Max(0f, WaveBannerTime - delta);
            if (ToastTime > 0f) ToastTime = Mathf.Max(0f, ToastTime - delta);

            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                var p = Particles[i];
                p.Life -= delta;
                if (p.Life <= 0f)
                {
                    Particles.RemoveAt(i);
                    continue;
                }
                if (!p.Ring)
                {
                    p.Pos += p.Vel * delta;
                    p.Vel *= Mathf.Pow(0.18f, delta);
                }
                Particles[i] = p;
            }

            for (int i = Texts.Count - 1; i >= 0; i--)
            {
                var t = Texts[i];
                t.Life -= delta;
                if (t.Life <= 0f)
                {
                    Texts.RemoveAt(i);
                    continue;
                }
                t.Pos.y -= 24f * delta;
                Texts[i] = t;
            }
        }

        public Vector2 ShakeOffset()
        {
            if (ShakeTime <= 0f) return Vector2.zero;
            float s = ShakeIntensity * Mathf.Clamp01(ShakeTime * 4f);
            return new Vector2(Random.Range(-s, s), Random.Range(-s, s));
        }
    }
}
