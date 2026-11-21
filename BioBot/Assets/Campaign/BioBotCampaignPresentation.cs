using UnityEngine;

namespace BioBot
{
    public sealed partial class BioBotCampaign
    {
        private static readonly Rect BoardViewport = new Rect(10, 73, 748, 288);
        private static readonly Color Seam = new Color32(63, 88, 82, 255);
        private static readonly Color Ivory = new Color32(239, 239, 222, 255);
        private Texture2D ringTexture;
        private Texture2D shadowTexture;

        // Compose the hidden board at one-third resolution. Blurring after
        // composition lets colors bleed into the water and into nearby objects.
        private const int LensWidth = 205;
        private const int LensHeight = 40;
        private Texture2D waterLens;
        private Color32[] lensObjects;
        private Color32[] lensHorizontal;
        private Color32[] lensFinal;
        private Color32[] lensSprites;
        private CampaignMatch lensMatch;
        private Layer lensLayer;
        private float lensTime;

        private void DrawPoolBackdrop()
        {
            GUI.color = Color.white;
            if (estuary != null)
                GUI.DrawTexture(new Rect(0, 0, 768, 432), estuary, ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 0, 768, 432), new Color(0.03f, 0.07f, 0.08f, 0.34f));

            DrawBoardFace(Layer.Surface, 100f, CampaignMatch.SurfY0);
            DrawBoardFace(Layer.Deep, 76f, CampaignMatch.DeepY0);
            DrawCurrentLines();
            DrawLayerCaption(Layer.Surface, 204f);
            DrawLayerCaption(Layer.Deep, 348f);
        }

        private void DrawBoardFace(Layer layer, float x, float y)
        {
            const float width = CampaignMatch.Columns * CampaignMatch.CellW;
            const float height = CampaignMatch.Rows * CampaignMatch.CellH;
            bool active = layer == match.RobotLayer;
            bool locked = layer == Layer.Deep && !match.DeepEnabled;
            Color water = layer == Layer.Surface
                ? new Color32(37, 114, 110, 193)
                : new Color32(23, 63, 91, 212);
            if (!active) water = Color.Lerp(water, new Color32(28, 57, 76, 236), locked ? 0.94f : 0.73f);

            Matrix4x4 previous = GUI.matrix;
            Matrix4x4 face = Matrix4x4.Translate(new Vector3(x, y, 0f));
            face.m01 = -CampaignMatch.ShearX / CampaignMatch.CellH;
            GUI.matrix = previous * face;

            Fill(new Rect(-11, 3, width + 22, height + 5), new Color(0f, 0f, 0f, 0.53f));
            Fill(new Rect(-7, -3, width + 14, height + 7), WithAlpha(Ink, 0.67f));
            Fill(new Rect(0, 0, width, height), water);
            if (active)
            {
                for (int row = 0; row < CampaignMatch.Rows; row++)
                    for (int col = 0; col < CampaignMatch.Columns; col++)
                    {
                        var tile = new Rect(col * CampaignMatch.CellW, row * CampaignMatch.CellH,
                            CampaignMatch.CellW, CampaignMatch.CellH);
                        Color wash = (row + col) % 2 == 0
                            ? new Color(0.67f, 0.93f, 0.82f, 0.075f)
                            : new Color(0f, 0.065f, 0.11f, 0.07f);
                        Fill(tile, wash);
                        Fill(new Rect(tile.x, tile.y, tile.width, 0.65f), WithAlpha(Teal, 0.24f));
                        Fill(new Rect(tile.x, tile.y, 0.65f, tile.height), WithAlpha(Teal, 0.22f));
                        if (col == 0) Fill(new Rect(tile.x, tile.y + 5, 2, 14), WithAlpha(Gold, 0.56f));
                    }
            }

            float warning = !active && !locked ? match.DangerWarningLeft(layer) : 0f;
            if (!active && !locked) DrawWaterLens(layer, new Rect(0, 0, width, height));
            if (warning > 0f)
            {
                float fade = Mathf.Clamp01(warning * 2.5f);
                Fill(new Rect(0, 0, width, height), WithAlpha(Danger, 0.22f * fade));
                Border(new Rect(0, 0, width, height), WithAlpha(Danger, 0.86f * fade));
            }

            Color rim = active ? (layer == Layer.Surface ? Teal : Cyan) : Seam;
            Fill(new Rect(-7, -3, width + 14, 1.4f), rim);
            Fill(new Rect(-7, height + 2, width + 14, 1.4f), WithAlpha(rim, 0.55f));
            Fill(new Rect(-7, 1, 4, height), WithAlpha(rim, 0.37f));
            Fill(new Rect(width + 3, 1, 4, height), WithAlpha(rim, 0.37f));
            GUI.matrix = previous;

            if (warning > 0f)
            {
                Vector2 center = CampaignMatch.CellToPixel(layer, (CampaignMatch.Columns - 1) * 0.5f, (CampaignMatch.Rows - 1) * 0.5f);
                center.y -= 9f;
                float breath = 0.91f + 0.09f * Mathf.Sin(visualTime * 5f);
                float iconWidth = 110f * breath;
                float iconHeight = iconWidth * 0.9f;
                GUI.color = WithAlpha(Color.white, Mathf.Clamp01(warning * 2.5f));
                if (dangerTriangle != null)
                    GUI.DrawTexture(new Rect(center.x - iconWidth * 0.5f, center.y - iconHeight * 0.5f, iconWidth, iconHeight), dangerTriangle);

                var prompt = new Rect(center.x - 83f, center.y + iconHeight * 0.5f + 3f, 166f, 19f);
                Fill(prompt, Ink);
                Border(prompt, Danger);
                Label(prompt.x, prompt.y, prompt.width, prompt.height,
                    layer == Layer.Deep ? "BẤM [Q] ĐỂ LẶN" : "BẤM [Q] ĐỂ NỔI", dangerPrompt, Ivory);
            }

            if (locked) Label(69, 275, 520, 30, "Đáy biển mở ở màn 02", smallCenter, Muted);
        }

        private void DrawCurrentLines()
        {
            Layer layer = match.RobotLayer;
            for (int i = 0; i < 22; i++)
            {
                float drift = Mathf.Repeat(i * 0.127f + visualTime * (0.029f + i % 3 * 0.009f), 1f);
                float alpha = Mathf.Sin(drift * Mathf.PI);
                Vector2 pos = CampaignMatch.CellToPixel(layer, 0.1f + drift * 9.75f,
                    (i % 5) + Mathf.Sin(i * 9f) * 0.33f);
                if (layer == Layer.Surface)
                {
                    Fill(new Rect(pos.x - 7, pos.y + 2, 14, 1f), WithAlpha(Ivory, alpha * 0.25f));
                    Fill(new Rect(pos.x + 8, pos.y + 1, 5, 1f), WithAlpha(Teal, alpha * 0.20f));
                }
                else
                {
                    float flow = Mathf.Sin(visualTime * 1.35f + i) * 9f;
                    Fill(new Rect(pos.x + flow, pos.y - 3, 11f + i % 4 * 4f, 1f), WithAlpha(Cyan, alpha * 0.22f));
                    Fill(new Rect(pos.x + flow + 5, pos.y - 4, 3f, 1f), WithAlpha(Teal, alpha * 0.14f));
                }
            }
        }

        private void DrawLayerCaption(Layer layer, float y)
        {
            bool active = match.RobotLayer == layer;
            bool danger = match.HasDangerWarning(layer);
            Color accent = danger ? Danger : active ? (layer == Layer.Surface ? Teal : Cyan) : Muted;
            Surface(new Rect(26, y, 148, 16), Ink, WithAlpha(accent, danger ? 0.94f : 0.40f), 3f);
            DrawCircle(new Vector2(35, y + 8), danger ? 6f : 4.5f, accent);
            string label = LayerName(layer);
            if (danger && active) label += " · NGUY HIỂM";
            else if (active) label += "  ·  BIO-BOT";
            else if (match.DeepEnabled) label += "  ·  Q";
            Label(43, y + 1, 128, 14, label, small, accent);
        }

        private void DrawTileHighlight(Layer layer, int col, int row, Color accent)
        {
            Vector2 corner = CampaignMatch.CellCorner(layer, col, row);
            Matrix4x4 previous = GUI.matrix;
            Matrix4x4 tile = Matrix4x4.Translate(corner);
            tile.m01 = -CampaignMatch.ShearX / CampaignMatch.CellH;
            GUI.matrix = previous * tile;
            Rect bounds = new Rect(1, 1, CampaignMatch.CellW - 2, CampaignMatch.CellH - 2);
            Fill(bounds, WithAlpha(accent, 0.20f));
            Border(bounds, WithAlpha(accent, 0.85f));
            GUI.matrix = previous;
        }

        private static Rect UnitRect(Vector2 contact, float width, float height) =>
            new Rect(contact.x - width * 0.5f, contact.y - height * 0.61f, width, height);

        private void DrawWaterUnit(int row, Rect body, Vector2 contact, Layer layer, float fps, int offset, Color tint, bool grounded)
        {
            if (atlas == null) return;
            Rect uv = AtlasUV(row, SpriteFrame(fps, offset), 4);
            DrawWaterSprite(atlas, uv, body, contact, layer, tint, grounded, visualTime * 2f + offset);
        }

        private void DrawWaterSprite(Texture2D texture, Rect uv, Rect body, Vector2 contact, Layer layer,
            Color tint, bool grounded, float phase)
        {
            if (layer == Layer.Deep)
            {
                // Floor contact stays put while a swimmer drifts above its softer shadow.
                Vector2 floor = contact + new Vector2(0f, grounded ? 4f : 10f);
                DrawShadow(floor, body.width * (grounded ? 0.84f : 0.60f));
                if (grounded) body.y += body.height * 0.09f;
                Color blue = tint * new Color(0.70f, 0.88f, 0.99f, 1f);
                GUI.color = blue;
                GUI.DrawTextureWithTexCoords(body, texture, uv);
                float caustic = Mathf.Lerp(0.22f, 0.73f, Mathf.Sin(phase * 0.63f) * 0.5f + 0.5f);
                DrawSpriteSlice(texture, uv, body, caustic, caustic + 0.04f, 0f,
                    WithAlpha(new Color(0.79f, 1f, 1f), tint.a * 0.30f));
                return;
            }

            float cut = Mathf.Clamp01((contact.y - body.y) / body.height);
            DrawWake(contact, body.width * 0.80f, phase, tint.a * 0.58f);

            // Fade the silhouette into the water in two refracted bands. The
            // cut is tied to the tile, so bobbing never moves the waterline.
            float middle = Mathf.Min(1f, cut + 0.12f);
            Color wet = tint * new Color(0.40f, 0.78f, 0.83f, 0.56f);
            DrawSpriteSlice(texture, uv, body, middle, 1f, Mathf.Sin(phase + 0.8f) * 1.7f, WithAlpha(wet, wet.a * 0.57f));
            DrawSpriteSlice(texture, uv, body, cut, middle, Mathf.Sin(phase) * 0.95f, wet);
            DrawSpriteSlice(texture, uv, body, 0f, cut, 0f, tint);

            // Foam crosses in front of the hull and feet at the actual contact line.
            float foamX = contact.x - body.width * 0.32f;
            float wave = Mathf.Sin(phase) * 2f;
            Fill(new Rect(foamX + wave, contact.y, body.width * 0.30f, 1.1f), WithAlpha(Ivory, tint.a * 0.68f));
            Fill(new Rect(foamX + body.width * 0.36f - wave, contact.y + 1f, body.width * 0.24f, 1f),
                WithAlpha(Teal, tint.a * 0.82f));
        }

        private static void DrawSpriteSlice(Texture2D texture, Rect uv, Rect body, float from, float to, float offset, Color tint)
        {
            if (to <= from) return;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(new Rect(body.x + offset, body.y + from * body.height, body.width, (to - from) * body.height), texture,
                new Rect(uv.x, uv.y + (1f - to) * uv.height, uv.width, (to - from) * uv.height));
        }

        private void DrawWake(Vector2 p, float width, float phase, float strength)
        {
            DrawRing(p + Vector2.up * 0.5f, width * 0.98f, width * 0.25f, WithAlpha(Cyan, strength * 0.73f));
            float age = Mathf.Repeat(visualTime * 0.9f + phase * 0.17f, 1f);
            DrawRing(p, width * (0.88f + age * 0.48f), width * (0.22f + age * 0.12f),
                WithAlpha(Teal, strength * (1f - age) * 0.56f));
        }

        private void DrawSplash(Vector2 contact, float progress, float strength)
        {
            progress = Mathf.Clamp01(progress);
            DrawRing(contact, 18f + progress * 35f, 5f + progress * 8f,
                WithAlpha(Ivory, (1f - progress) * strength));
            for (int i = 0; i < 5; i++)
            {
                float spread = (i - 2) * (2f + progress * 6f);
                float lift = Mathf.Sin(progress * Mathf.PI) * (9f + 3f * (i % 2));
                Fill(new Rect(contact.x + spread, contact.y - lift, 1.5f, 2.5f),
                    WithAlpha(Cyan, (1f - progress) * strength));
            }
        }

        private void DrawBubbles(Vector2 p, float phase, float strength)
        {
            for (int i = 0; i < 3; i++)
            {
                float age = Mathf.Repeat(visualTime * 0.84f + i / 3f + phase * 0.11f, 1f);
                Vector2 bubble = p + new Vector2(Mathf.Sin(age * 7f + i) * 5f, -2f - age * 20f);
                float size = 2f + age * 4f;
                DrawRing(bubble, size, size, WithAlpha(Cyan, (1f - age) * strength));
            }
        }

        private void DrawTacticalEffects()
        {
            foreach (var pull in match.SquidPulls)
            {
                float age = Mathf.Clamp01(pull.Age / CampaignMatch.SquidPullSeconds);
                float travel = Mathf.SmoothStep(0f, 1f, age);
                Vector2 start = CampaignMatch.CellToPixel(pull.From, pull.X, pull.Row);
                Vector2 end = CampaignMatch.CellToPixel(pull.To, pull.X, pull.Row);
                Vector2 p = Vector2.Lerp(start, end, travel);
                bool downward = pull.From != pull.To && pull.To == Layer.Deep;
                Vector2 squid = p + new Vector2(7f, downward ? 8f : -10f);

                if (pull.From == Layer.Surface)
                    DrawSplash(start, age, 0.90f);
                else
                    DrawBubbles(start, pull.Row, 1f - age);
                if (pull.From != pull.To && age > 0.78f)
                {
                    if (pull.To == Layer.Surface) DrawSplash(end, (age - 0.78f) / 0.22f, 0.88f);
                    else DrawBubbles(end, pull.Row, age);
                }

                float alpha = pull.Enemy.Health <= 0 ? 1f - age : 1f;
                DrawSprite(CampaignCatalog.EnemySpriteRow(pull.Enemy.Spec), UnitRect(p, 37f, 37f),
                    0f, 0, WithAlpha(Ivory, alpha));
                Matrix4x4 previous = GUI.matrix;
                if (downward) GUIUtility.RotateAroundPivot(180f, squid);
                DrawSprite(CampaignCatalog.AllySpriteRow[pull.AllySpec], Around(squid, 44f),
                    12f, 0, WithAlpha(Color.white, 1f - Mathf.Pow(age, 4f)));
                GUI.matrix = previous;
            }

            foreach (var shot in match.LitterShots)
            {
                float age = Mathf.Clamp01(shot.Age / CampaignMatch.LitterFlightSeconds);
                Vector2 end = CampaignMatch.CellToPixel(shot.Layer, shot.Col, shot.Row);
                Vector2 p = Vector2.Lerp(shot.Start, end, age);
                p.y -= 34f * 4f * age * (1f - age);
                float trailAge = Mathf.Max(0f, age - 0.10f);
                Vector2 trail = Vector2.Lerp(shot.Start, end, trailAge);
                trail.y -= 34f * 4f * trailAge * (1f - trailAge);
                DrawCircle(trail, 6f, WithAlpha(Cyan, 0.45f));
                Matrix4x4 previous = GUI.matrix;
                GUIUtility.RotateAroundPivot(age * 240f, p);
                DrawSprite(CampaignCatalog.TrashSpriteRow(CampaignCatalog.ClutterId), Around(p, 22f), 9f, 0, Color.white);
                GUI.matrix = previous;
            }
        }

        private void DrawDebris(CampaignMatch.Waste waste, Layer layer, Vector2 p, float scale, int offset)
        {
            TrashEffect effect = CampaignCatalog.Trash[waste.Spec].Effect;
            float size = 40f * scale;
            Rect body = UnitRect(p, size, size);
            float bob = Mathf.Sin(visualTime * 2.5f + waste.BobPhase);
            if (effect == TrashEffect.Spill)
            {
                body.y += size * 0.09f;
                DrawSprite(CampaignCatalog.TrashSpriteRow(waste.Spec), body, 4, offset, Color.white);
                return;
            }

            bool grounded = effect is TrashEffect.Shell or TrashEffect.Ballast or TrashEffect.Battery or TrashEffect.Vent or TrashEffect.Clutter;
            if (!grounded) body.y += bob * 1.7f;
            if (effect == TrashEffect.Float) body.y += size * 0.12f;
            DrawWaterUnit(CampaignCatalog.TrashSpriteRow(waste.Spec), body, p, layer, 5, offset, Color.white, grounded);

            if (waste.Active)
                DrawBubbles(p, waste.BobPhase, 0.9f);
            if (effect == TrashEffect.Filament && match.WasteAtRobot != waste)
                Label(p.x - 18, p.y - 18, 36f, 12f, "CỨU", smallCenter, Gold);
        }

        private void PrepareWaterLens()
        {
            if (atlas == null || !atlas.isReadable) return;
            lensSprites = atlas.GetPixels32();
            int length = LensWidth * LensHeight;
            lensObjects = new Color32[length];
            lensHorizontal = new Color32[length];
            lensFinal = new Color32[length];
            waterLens = new Texture2D(LensWidth, LensHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private void DrawWaterLens(Layer layer, Rect bounds)
        {
            if (waterLens == null) return;
            bool lookingDown = layer == Layer.Deep;
            if (lensMatch != match || lensLayer != layer || visualTime - lensTime >= 0.056f)
            {
                ComposeWaterLens(layer);
                lensMatch = match;
                lensLayer = layer;
                lensTime = visualTime;
            }

            // Each horizontal sample slides differently as the water refracts
            // what lies beyond it. Looking up has the strongest distortion.
            const int bands = 20;
            GUI.color = Color.white;
            for (int band = 0; band < bands; band++)
            {
                float v = band / (float)bands;
                float shift = Mathf.Sin(v * 11f + visualTime * 1.8f) * (lookingDown ? 0.009f : 0.020f)
                    + Mathf.Sin(v * 18f - visualTime * 1.3f) * (lookingDown ? 0.004f : 0.010f);
                float h = bounds.height / bands;
                GUI.DrawTextureWithTexCoords(new Rect(bounds.x, bounds.y + band * h, bounds.width, h),
                    waterLens, new Rect(0.035f + shift, 1f - v - 1f / bands, 0.93f, 1f / bands));
            }
        }

        private void ComposeWaterLens(Layer layer)
        {
            bool lookingDown = layer == Layer.Deep;
            Color32 near = lookingDown ? new Color32(10, 32, 47, 255) : new Color32(61, 106, 113, 255);
            Color32 far = lookingDown ? new Color32(4, 15, 28, 255) : new Color32(24, 59, 81, 255);
            for (int y = 0; y < LensHeight; y++)
            {
                float depth = y / (LensHeight - 1f);
                Color32 water = Color32.Lerp(far, near, depth);
                for (int x = 0; x < LensWidth; x++)
                {
                    float u = x / (LensWidth - 1f);
                    float wave = Mathf.Sin(u * 17f + depth * 11f + visualTime * 1.2f)
                        * Mathf.Sin(depth * 15f - u * 7f - visualTime * 0.83f);
                    float shine = Mathf.Pow(Mathf.Max(0f, wave), 2f);
                    float vignette = (1f - Mathf.Abs(u * 2f - 1f) * 0.28f);
                    int light = Mathf.RoundToInt(shine * (lookingDown ? 5f : 40f) * vignette);
                    lensObjects[y * LensWidth + x] = new Color32(
                        (byte)(water.r * vignette + light * 0.68f),
                        (byte)(water.g * vignette + light),
                        (byte)(water.b * vignette + light * 0.87f), 255);
                }
            }

            int side = (int)layer;
            for (int row = 0; row < CampaignMatch.Rows; row++)
                for (int col = 0; col < CampaignMatch.Columns; col++)
                {
                    var waste = match.Trash[side, row, col];
                    if (waste != null)
                        StampIntoWater(CampaignCatalog.TrashSpriteRow(waste.Spec), col, row, 39f, waste.BobPhase, lookingDown);
                    var ally = match.Allies[side, row, col];
                    if (ally != null)
                        StampIntoWater(CampaignCatalog.AllySpriteRow[ally.Spec], col, row, 43f, ally.Phase, lookingDown);
                }

            foreach (var enemy in match.Enemies)
                if (enemy.Layer == layer && enemy.SquidPull == null)
                    StampIntoWater(CampaignCatalog.EnemySpriteRow(enemy.Spec), enemy.X, enemy.Row,
                        CampaignCatalog.Enemies[enemy.Spec].Trait == Trait.Boss ? 61f : 45f, enemy.WalkPhase, lookingDown);
            foreach (var bottle in match.SlidingBottles)
                if (bottle.Layer == layer)
                    StampIntoWater(34, bottle.X, bottle.Row, 37f, bottle.X, lookingDown);

            for (int y = 0; y < LensHeight; y++)
                for (int x = 0; x < LensWidth; x++)
                {
                    int row = y * LensWidth;
                    lensHorizontal[row + x] = WaterBlur(lensObjects[row + Mathf.Max(0, x - 2)],
                        lensObjects[row + Mathf.Max(0, x - 1)], lensObjects[row + x],
                        lensObjects[row + Mathf.Min(LensWidth - 1, x + 1)],
                        lensObjects[row + Mathf.Min(LensWidth - 1, x + 2)]);
                }
            for (int y = 0; y < LensHeight; y++)
                for (int x = 0; x < LensWidth; x++)
                    lensFinal[y * LensWidth + x] = WaterBlur(
                        lensHorizontal[Mathf.Max(0, y - 2) * LensWidth + x],
                        lensHorizontal[Mathf.Max(0, y - 1) * LensWidth + x],
                        lensHorizontal[y * LensWidth + x],
                        lensHorizontal[Mathf.Min(LensHeight - 1, y + 1) * LensWidth + x],
                        lensHorizontal[Mathf.Min(LensHeight - 1, y + 2) * LensWidth + x]);

            waterLens.SetPixels32(lensFinal);
            waterLens.Apply(false);
        }

        private void StampIntoWater(int sprite, float col, int row, float logicalSize, float phase, bool lookingDown)
        {
            int frame = SpriteFrame(5f, row);
            int atlasWidth = atlas.width;
            int frameWidth = atlasWidth / 4;
            int frameHeight = atlas.height / CampaignCatalog.TotalAtlasRows;
            int spriteX = frame * frameWidth;
            int spriteY = (CampaignCatalog.TotalAtlasRows - 1 - sprite) * frameHeight;
            int size = Mathf.RoundToInt(logicalSize / 3f);
            int centerX = Mathf.RoundToInt((col + 0.5f) * LensWidth / CampaignMatch.Columns);
            int centerY = Mathf.RoundToInt(LensHeight - (row + 0.5f) * LensHeight / CampaignMatch.Rows);
            int px = centerX - size / 2;
            int py = centerY - Mathf.RoundToInt(size * 0.39f + Mathf.Sin(visualTime * 1.7f + phase));
            float visibility = lookingDown ? 0.31f : 0.40f;
            for (int y = Mathf.Max(0, py); y < Mathf.Min(LensHeight, py + size); y++)
            {
                int sourceRow = (spriteY + (y - py) * frameHeight / size) * atlasWidth + spriteX;
                int targetRow = y * LensWidth;
                for (int x = Mathf.Max(0, px); x < Mathf.Min(LensWidth, px + size); x++)
                {
                    Color32 spritePixel = lensSprites[sourceRow + (x - px) * frameWidth / size];
                    if (spritePixel.a == 0) continue;
                    int target = targetRow + x;
                    var filtered = lookingDown
                        ? new Color32((byte)(spritePixel.r * 0.24f + 8), (byte)(spritePixel.g * 0.34f + 18), (byte)(spritePixel.b * 0.44f + 25), 255)
                        : new Color32((byte)(spritePixel.r * 0.52f + 31), (byte)(spritePixel.g * 0.49f + 45), (byte)(spritePixel.b * 0.46f + 53), 255);
                    lensObjects[target] = Color32.Lerp(lensObjects[target], filtered, spritePixel.a / 255f * visibility);
                }
            }
        }

        private static Color32 WaterBlur(Color32 a, Color32 b, Color32 c, Color32 d, Color32 e) => new Color32(
            (byte)((a.r + 4 * b.r + 6 * c.r + 4 * d.r + e.r) / 16),
            (byte)((a.g + 4 * b.g + 6 * c.g + 4 * d.g + e.g) / 16),
            (byte)((a.b + 4 * b.b + 6 * c.b + 4 * d.b + e.b) / 16),
            255);

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private Color HudTint(Color color) => linearColorSpace ? color.linear : color;

        private void Surface(Rect bounds, Color face, Color outline, float radius = 5f)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(bounds, Texture2D.whiteTexture, ScaleMode.StretchToFill, true,
                0f, HudTint(outline), 0f, radius);
            bounds = new Rect(bounds.x + 1, bounds.y + 1, bounds.width - 2, bounds.height - 2);
            GUI.DrawTexture(bounds, Texture2D.whiteTexture, ScaleMode.StretchToFill, true,
                0f, HudTint(face), 0f, Mathf.Max(0f, radius - 1f));
        }

        private void DrawRing(Vector2 center, float width, float height, Color color)
        {
            GUI.color = HudTint(color);
            GUI.DrawTexture(new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height), ringTexture);
        }

        private void DrawShadow(Vector2 center, float width)
        {
            GUI.color = new Color(0f, 0.02f, 0.025f, 0.62f);
            GUI.DrawTexture(new Rect(center.x - width * 0.5f, center.y - width * 0.12f, width, width * 0.24f), shadowTexture);
        }

        private static Texture2D MakeRadialTexture(bool ring)
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var pos = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    float radius = pos.magnitude;
                    float alpha = ring ? Mathf.Clamp01(1f - Mathf.Abs(radius - 0.78f) * 7f)
                        : Mathf.Pow(Mathf.Clamp01(1f - radius), 1.7f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private void OnDestroy()
        {
            Destroy(circleTex);
            Destroy(ringTexture);
            Destroy(shadowTexture);
            Destroy(waterLens);
        }
    }
}
