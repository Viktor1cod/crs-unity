using System.Collections.Generic;
using System.Text;
using CRS.Core.Cars;
using UnityEngine;

namespace CRS.Game
{
    /// <summary>
    /// Temporary developer board: shows generated listings on screen so the build
    /// can be checked in the browser. Replaced by the real marketplace UI in week 3.
    /// Creates itself on game start, no scene setup needed.
    /// </summary>
    public sealed class DevListingBoard : MonoBehaviour
    {
        private const int ListingCount = 8;

        private ListingGenerator generator;
        private IReadOnlyList<Listing> listings;
        private Language lang = Language.Ru;
        private bool showTruth;
        private Vector2 scroll;
        private GUIStyle title, body, truth, button, toggle;

        /// <summary>
        /// The built-in runtime font has no Cyrillic glyphs in Web builds,
        /// so the board uses a bundled font from Assets/Resources/Fonts/Main.ttf.
        /// </summary>
        private const string FontResource = "Fonts/Main";
        private Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<DevListingBoard>() != null) return;
            var go = new GameObject(nameof(DevListingBoard));
            DontDestroyOnLoad(go);
            go.AddComponent<DevListingBoard>();
        }

        private void Awake()
        {
            lang = Application.systemLanguage == SystemLanguage.Russian ? Language.Ru : Language.En;
            generator = new ListingGenerator(System.Environment.TickCount);
            Refresh();
        }

        private void Refresh() => listings = generator.GenerateMany(ListingCount);

        private void EnsureStyles()
        {
            if (title != null) return;

            font = Resources.Load<Font>(FontResource);
            if (font == null)
                Debug.LogWarning($"DevListingBoard: font '{FontResource}' not found, Cyrillic will not render in Web builds.");

            int baseSize = Mathf.Max(14, Screen.height / 45);
            title = new GUIStyle(GUI.skin.label) { fontSize = baseSize + 4, fontStyle = FontStyle.Bold, wordWrap = true };
            body = new GUIStyle(GUI.skin.label) { fontSize = baseSize, wordWrap = true };
            truth = new GUIStyle(body);
            truth.normal.textColor = new Color(1f, 0.75f, 0.3f);
            button = new GUIStyle(GUI.skin.button) { fontSize = baseSize, padding = new RectOffset(16, 16, 8, 8) };
            toggle = new GUIStyle(GUI.skin.toggle) { fontSize = baseSize };

            if (font != null)
                foreach (var s in new[] { title, body, truth, button, toggle }) s.font = font;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (font != null) GUI.skin.font = font;
            float pad = 12;
            var area = new Rect(pad, pad, Screen.width - pad * 2, Screen.height - pad * 2);
            GUI.Box(area, GUIContent.none);

            GUILayout.BeginArea(new Rect(area.x + 10, area.y + 10, area.width - 20, area.height - 20));
            GUILayout.BeginHorizontal();
            GUILayout.Label(lang == Language.Ru ? "Объявления (dev)" : "Listings (dev)", title);
            GUILayout.FlexibleSpace();
            float h = title.fontSize * 2;
            if (GUILayout.Button(lang == Language.Ru ? "Обновить" : "Refresh", button, GUILayout.Height(h))) Refresh();
            if (GUILayout.Button(lang == Language.Ru ? "EN" : "RU", button, GUILayout.Height(h)))
                lang = lang == Language.Ru ? Language.En : Language.Ru;
            showTruth = GUILayout.Toggle(showTruth, lang == Language.Ru ? " Правда" : " Truth", toggle, GUILayout.Height(h));
            GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var l in listings) DrawListing(l);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawListing(Listing l)
        {
            var car = l.Car;
            GUILayout.Space(8);
            GUILayout.Label($"{car.Title(lang)} — ${l.AskingPrice:N0}", title);

            string km = lang == Language.Ru ? "км" : "km";
            GUILayout.Label($"{car.DisplayedMileage:N0} {km} · {SellerName(l.Seller)}", body);
            GUILayout.Label(l.Description(lang), body);

            if (showTruth)
            {
                var sb = new StringBuilder();
                sb.Append(lang == Language.Ru ? "Реальная цена: $" : "True value: $")
                  .Append(CarValuation.TrueValue(car).ToString("N0"));
                if (car.IsOdometerRolledBack)
                    sb.Append(lang == Language.Ru ? $" · пробег скручен (реально {car.RealMileage:N0})"
                                                  : $" · odometer rolled back (real {car.RealMileage:N0})");
                if (car.WasTaxi) sb.Append(lang == Language.Ru ? " · бывшее такси" : " · ex-taxi");
                foreach (var d in car.HiddenDefects)
                    sb.Append($" · {d.Component} x{d.Severity} (${d.RepairCost:N0})");
                GUILayout.Label(sb.ToString(), truth);
            }
        }

        private string SellerName(SellerType s)
        {
            switch (s)
            {
                case SellerType.Reseller: return lang == Language.Ru ? "перекуп" : "reseller";
                case SellerType.Urgent: return lang == Language.Ru ? "срочная продажа" : "urgent sale";
                default: return lang == Language.Ru ? "частник" : "private seller";
            }
        }
    }
}
