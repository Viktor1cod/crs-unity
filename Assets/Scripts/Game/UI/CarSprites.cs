using System.Collections.Generic;
using CRS.Core.Cars;
using UnityEngine;

namespace CRS.Game.UI
{
    /// <summary>
    /// Simple side-view car pictures drawn in code, one per body type and paint color.
    /// Placeholder until real car art is added.
    /// </summary>
    internal static class CarSprites
    {
        private const int W = 200;
        private const int H = 100;

        private static readonly Color32[] Paints =
        {
            new Color32(196, 48, 43, 255),   // red
            new Color32(232, 234, 237, 255), // white
            new Color32(44, 46, 52, 255),    // black
            new Color32(160, 166, 175, 255), // silver
            new Color32(41, 98, 186, 255),   // blue
            new Color32(46, 125, 74, 255),   // green
            new Color32(230, 180, 40, 255),  // yellow
            new Color32(122, 28, 46, 255),   // cherry
        };

        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite Get(BodyType body, int seed)
        {
            int paint = (seed & int.MaxValue) % Paints.Length;
            int key = (int)body * 100 + paint;
            if (Cache.TryGetValue(key, out var sprite)) return sprite;
            sprite = Draw(body, Paints[paint]);
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Draw(BodyType body, Color32 paint)
        {
            var px = new Color32[W * H];
            var clear = new Color32(0, 0, 0, 0);
            var glass = new Color32(150, 200, 235, 255);
            var tire = new Color32(25, 25, 28, 255);
            var rim = new Color32(170, 175, 182, 255);
            var headlight = new Color32(255, 230, 140, 255);

            void FillRect(int x0, int y0, int x1, int y1, Color32 c)
            {
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(H, y1); y++)
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(W, x1); x++)
                        px[y * W + x] = c;
            }

            void FillCircle(int cx, int cy, int r, Color32 c)
            {
                for (int y = cy - r; y <= cy + r; y++)
                    for (int x = cx - r; x <= cx + r; x++)
                        if (x >= 0 && x < W && y >= 0 && y < H && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                            px[y * W + x] = c;
            }

            // Car faces right. y = 0 is the bottom of the texture.
            int sill = 18, beltline, roof, cabinFrom, cabinTo, wheelR = 15, wheelY = 22;
            switch (body)
            {
                case BodyType.Suv: beltline = 56; roof = 88; cabinFrom = 22; cabinTo = 150; wheelR = 18; wheelY = 24; break;
                case BodyType.Crossover: beltline = 52; roof = 82; cabinFrom = 32; cabinTo = 146; wheelR = 17; wheelY = 23; break;
                case BodyType.Pickup: beltline = 54; roof = 86; cabinFrom = 96; cabinTo = 150; wheelR = 18; wheelY = 24; break;
                case BodyType.Muscle: beltline = 44; roof = 66; cabinFrom = 62; cabinTo = 126; break;
                default: beltline = 46; roof = 72; cabinFrom = 50; cabinTo = 140; break;
            }

            FillRect(8, sill, 192, beltline, paint);
            FillRect(cabinFrom, beltline, cabinTo, roof, paint);

            int mid = (cabinFrom + cabinTo) / 2;
            FillRect(cabinFrom + 6, beltline + 4, mid - 3, roof - 6, glass);
            FillRect(mid + 3, beltline + 4, cabinTo - 8, roof - 6, glass);

            if (body == BodyType.Pickup) FillRect(8, beltline, 92, beltline + 6, paint);
            FillRect(184, beltline - 12, 192, beltline - 4, headlight);

            foreach (int wheelX in new[] { 42, 160 })
            {
                FillCircle(wheelX, wheelY, wheelR + 3, clear);
                FillCircle(wheelX, wheelY, wheelR, tire);
                FillCircle(wheelX, wheelY, wheelR / 3, rim);
            }

            var texture = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(px);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
