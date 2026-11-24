using UnityEngine;

namespace CRS.Game
{
    /// <summary>Lifecycle shared by desktop, browser and the VK WebView.</summary>
    public sealed class PlatformRuntime : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int CRS_IsRussian();
#endif
        public static bool RussianByDefault
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return CRS_IsRussian() != 0;
#else
                return Application.systemLanguage == SystemLanguage.Russian;
#endif
            }
        }

        private bool pageHidden;
        private bool applicationPaused;
        private bool userMuted;
        private AudioSource source;
        private AudioClip purchase, sale, repair;
        public static PlatformRuntime Instance { get; private set; }
        public bool Muted => userMuted;

        public static void EnsureCreated()
        {
            if (Instance != null) return;
            var go = new GameObject("PlatformRuntime");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<PlatformRuntime>();
        }

        private void Awake()
        {
            Instance = this;
            userMuted = PlayerPrefs.GetInt("crs.soundMuted", 0) != 0;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            purchase = Tone("Purchase", 440, 330);
            sale = Tone("Sale", 660, 880);
            repair = Tone("Repair", 220, 440);
            ApplyMute();
        }

        public void ToggleSound()
        {
            userMuted = !userMuted;
            PlayerPrefs.SetInt("crs.soundMuted", userMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMute();
        }

        // Called by the WebGL template using Unity SendMessage.
        [UnityEngine.Scripting.Preserve]
        public void SetPageHidden(string hidden) { pageHidden = hidden == "1"; ApplyMute(); }
        private void OnApplicationPause(bool paused) { applicationPaused = paused; ApplyMute(); }
        private void ApplyMute()
        {
            AudioListener.pause = userMuted || pageHidden || applicationPaused;
            if (AudioListener.pause && source != null) source.Stop();
        }

        public void PurchaseSound() => Play(purchase);
        public void SaleSound() => Play(sale);
        public void RepairSound() => Play(repair);
        private void Play(AudioClip clip) { if (!AudioListener.pause) source.PlayOneShot(clip, 0.15f); }

        // Original short synthesized effects; no external audio assets required.
        private static AudioClip Tone(string name, float first, float second)
        {
            const int rate = 22050;
            const int length = 6615;
            var samples = new float[length];
            double phase = 0;
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float envelope = Mathf.Min(t * 30, 1) * (1 - t) * (1 - t);
                phase += 2 * System.Math.PI * (t < 0.5f ? first : second) / rate;
                samples[i] = (float)System.Math.Sin(phase) * envelope;
            }
            var clip = AudioClip.Create(name, length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            Destroy(purchase); Destroy(sale); Destroy(repair);
            if (Instance == this) { Instance = null; AudioListener.pause = false; }
        }
    }
}
