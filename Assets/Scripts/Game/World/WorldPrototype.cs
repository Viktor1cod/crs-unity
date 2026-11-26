using System;
using System.Collections.Generic;
using CRS.Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CRS.Game.World
{
    /// <summary>A deliberately separate driving playground; never changes the trade session.</summary>
    public sealed class WorldPrototype : MonoBehaviour
    {
        private readonly List<Camera> previousCameras = new List<Camera>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Transform> destinations = new List<Transform>();
        private readonly string[] names = { "Свой гараж", "Авторынок", "СТО", "Автомойка" };
        private Rigidbody car;
        private Camera worldCamera;
        private Text status;
        private Action onClose;
        private float speed;
        private bool left, right, forward, reverse;
        private const float TopSpeed = 16f;
        private static readonly Vector3 Spawn = new Vector3(-40, 0.65f, -35);

        public static void Open(Action onClose)
        {
            var go = new GameObject("WorldPrototype");
            var world = go.AddComponent<WorldPrototype>();
            world.onClose = onClose;
            try { world.Build(); }
            catch
            {
                go.SetActive(false);
                world.RestoreCameras();
                Destroy(go);
                throw;
            }
        }

        private Material Paint(Color color)
        {
            // A Resources material keeps its shader and used variant in player builds.
            // Shader.Find alone works in the Editor but its shader can be stripped in WebGL.
            var template = Resources.Load<Material>("World/District");
            if (template == null || template.shader == null)
                throw new InvalidOperationException("District material is missing from the build.");
            var material = new Material(template);
            material.color = color;
            materials.Add(material);
            return material;
        }

        private GameObject Box(string name, Vector3 position, Vector3 size, Material material, bool solid = true, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) go.GetComponent<Collider>().enabled = false;
            return go;
        }

        private void Build()
        {
            foreach (var camera in Camera.allCameras)
                if (camera.enabled) { previousCameras.Add(camera); camera.enabled = false; }
            var ground = Paint(new Color(.28f, .35f, .30f));
            var road = Paint(new Color(.15f, .18f, .21f));
            var line = Paint(new Color(.83f, .77f, .56f));
            var wall = Paint(new Color(.39f, .43f, .46f));
            var roof = Paint(new Color(.23f, .28f, .33f));
            var glass = Paint(new Color(.17f, .30f, .37f));
            var blue = Paint(new Color(.26f, .61f, .78f));
            var orange = Paint(new Color(.91f, .55f, .26f));
            Box("Ground", new Vector3(0, -.5f, 0), new Vector3(140, 1, 120), ground);
            foreach (float x in new[] { -40f, 40f })
                Box("NorthSouth road", new Vector3(x, .01f, 0), new Vector3(12, .02f, 82), road, false);
            foreach (float z in new[] { -35f, 35f })
                Box("EastWest road", new Vector3(0, .01f, z), new Vector3(92, .02f, 12), road, false);
            for (int x = -30; x <= 30; x += 10)
                foreach (float z in new[] { -35f, 35f })
                    Box("Lane marking", new Vector3(x, .04f, z), new Vector3(4, .02f, .18f), line, false);
            for (int z = -20; z <= 20; z += 10)
                foreach (float x in new[] { -40f, 40f })
                    Box("Lane marking", new Vector3(x, .04f, z), new Vector3(.18f, .02f, 4), line, false);
            Box("North fence", new Vector3(0, 1, 59), new Vector3(140, 2, 1), wall);
            Box("South fence", new Vector3(0, 1, -59), new Vector3(140, 2, 1), wall);
            Box("West fence", new Vector3(-69, 1, 0), new Vector3(1, 2, 120), wall);
            Box("East fence", new Vector3(69, 1, 0), new Vector3(1, 2, 120), wall);
            var positions = new[] { new Vector3(-22, 0, -17), new Vector3(22, 0, -17), new Vector3(22, 0, 17), new Vector3(-22, 0, 17) };
            for (int i = 0; i < positions.Length; i++)
            {
                var p = positions[i];
                Box(names[i], p + Vector3.up * 2, new Vector3(16, 4, 12), i == 0 ? blue : wall);
                Box("Roof", p + Vector3.up * 4.2f, new Vector3(17, .4f, 13), roof, false);
                float approach = p.z < 0 ? -1 : 1;
                var marker = Box("Destination", p + new Vector3(0, .06f, approach * 10), new Vector3(8, .04f, 5), orange, false);
                destinations.Add(marker.transform);
                var sign = new GameObject("Sign");
                sign.transform.SetParent(transform, false);
                sign.transform.position = p + new Vector3(0, 5, 0);
                sign.transform.rotation = Quaternion.Euler(55, 0, 0);
                var text = sign.AddComponent<TextMesh>();
                text.text = names[i]; text.font = Ui.Font; text.fontSize = 64; text.characterSize = .12f;
                text.anchor = TextAnchor.MiddleCenter; text.color = Color.white;
                sign.GetComponent<MeshRenderer>().sharedMaterial = Ui.Font.material;
            }
            var vehicle = new GameObject("Test car");
            vehicle.transform.SetParent(transform, false);
            vehicle.transform.position = Spawn;
            var collider = vehicle.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.9f, 1.2f, 3.8f);
            car = vehicle.AddComponent<Rigidbody>();
            car.mass = 1000; car.useGravity = false;
            car.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            car.interpolation = RigidbodyInterpolation.Interpolate;
            car.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Box("Body", Vector3.zero, new Vector3(1.8f, .7f, 3.6f), blue, false, vehicle.transform);
            Box("Cabin", new Vector3(0, .6f, -.2f), new Vector3(1.5f, .65f, 1.8f), glass, false, vehicle.transform);
            Box("Hood", new Vector3(0, .4f, 1.2f), new Vector3(1.7f, .15f, .9f), blue, false, vehicle.transform);
            Box("Headlights", new Vector3(0, .1f, 1.82f), new Vector3(1.5f, .2f, .05f), line, false, vehicle.transform);
            foreach (float x in new[] { -1f, 1f })
                foreach (float z in new[] { -1.1f, 1.1f })
                    Box("Wheel", new Vector3(x, -.25f, z), new Vector3(.3f, .6f, .7f), roof, false, vehicle.transform);
            var cameraObject = new GameObject("District camera");
            cameraObject.transform.SetParent(transform, false);
            worldCamera = cameraObject.AddComponent<Camera>();
            worldCamera.orthographic = true; worldCamera.orthographicSize = 24;
            worldCamera.nearClipPlane = .1f; worldCamera.farClipPlane = 150;
            worldCamera.clearFlags = CameraClearFlags.SolidColor; worldCamera.backgroundColor = ground.color;
            worldCamera.transform.rotation = Quaternion.Euler(55, 0, 0);
            MakeUI();
            Follow(true);
        }

        private void MakeUI()
        {
            var root = new GameObject("District UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 600); scaler.matchWidthOrHeight = .5f;
            var top = Ui.CreateImage("Top", root.transform, new Color(.07f, .10f, .13f, .94f));
            top.rectTransform.anchorMin = new Vector2(0, 1); top.rectTransform.anchorMax = Vector2.one;
            top.rectTransform.pivot = new Vector2(.5f, 1); top.rectTransform.sizeDelta = new Vector2(0, 130);
            Ui.Vertical(top.gameObject, 4, Ui.Pad(12));
            var buttons = Ui.CreateRect("Actions", top.transform); Ui.Horizontal(buttons.gameObject, 8);
            Ui.CreateButton("Return", buttons, 20, Theme.Tab, Close, out var back); back.text = "К торговле";
            Ui.CreateButton("Reset", buttons, 20, Theme.Tab, ResetCar, out var reset); reset.text = "К гаражу";
            status = Ui.CreateText("Status", top.transform, 19, Color.white);
            var controls = Ui.CreateRect("Driving controls", root.transform);
            controls.anchorMin = Vector2.zero; controls.anchorMax = new Vector2(1, 0);
            controls.pivot = new Vector2(.5f, 0); controls.sizeDelta = new Vector2(0, 90);
            Ui.Horizontal(controls.gameObject, 8, Ui.Pad(12));
            Hold(controls, "←", value => left = value);
            Hold(controls, "→", value => right = value);
            Ui.Layout(Ui.CreateRect("Space", controls), flexibleWidth: 1);
            Hold(controls, "Назад", value => reverse = value);
            Hold(controls, "Газ", value => forward = value);
        }

        private void Hold(Transform parent, string label, Action<bool> changed)
        {
            var button = Ui.CreateButton(label, parent, 23, Theme.Tab, () => {}, out var text);
            text.text = label; Ui.Layout(button, minWidth: 70, minHeight: 60);
            button.gameObject.AddComponent<DriveHoldButton>().Changed = changed;
        }

        private void FixedUpdate()
        {
            if (car == null) return;
            float throttle = (forward ? 1 : 0) - (reverse ? 1 : 0);
            float steering = (right ? 1 : 0) - (left ? 1 : 0);
#if ENABLE_INPUT_SYSTEM
            var keys = Keyboard.current;
            if (keys != null)
            {
                throttle += (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
                steering += (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
            }
#endif
            throttle = Mathf.Clamp(throttle, -1, 1); steering = Mathf.Clamp(steering, -1, 1);
            float actualSpeed = Vector3.Dot(car.linearVelocity, car.transform.forward);
            speed = Mathf.MoveTowards(actualSpeed, throttle * (throttle < 0 ? 7 : TopSpeed), (throttle == 0 ? 9 : 12) * Time.fixedDeltaTime);
            car.linearVelocity = car.transform.forward * speed;
            car.angularVelocity = Vector3.zero;
            float turn = steering * Mathf.Clamp(speed / 7, -1, 1) * 90 * Time.fixedDeltaTime;
            car.MoveRotation(car.rotation * Quaternion.Euler(0, turn, 0));
        }

        private void LateUpdate()
        {
            if (car == null) return;
            Follow(false);
            int nearest = 0; float distance = float.MaxValue;
            for (int i = 0; i < destinations.Count; i++)
            {
                float d = Vector3.Distance(car.position, destinations[i].position);
                if (d < distance) { distance = d; nearest = i; }
            }
            status.text = distance < 7 ? names[nearest] + " · место встречи. Сервисы подключим следующим этапом." :
                "Район · WASD / стрелки или кнопки снизу\n" + names[nearest] + ": " + Mathf.RoundToInt(distance) + " м";
        }

        private void Follow(bool snap)
        {
            var target = car.position + new Vector3(0, 32, -22.4f);
            worldCamera.transform.position = snap ? target : Vector3.Lerp(worldCamera.transform.position, target, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
        }
        private void ClearInput() { left = right = forward = reverse = false; }
        private void OnApplicationFocus(bool focus) { if (!focus) { ClearInput(); if (car != null) car.linearVelocity = Vector3.zero; } }
        private void OnApplicationPause(bool pause) { if (pause) OnApplicationFocus(false); }
        private void ResetCar() { ClearInput(); speed = 0; car.linearVelocity = Vector3.zero; car.angularVelocity = Vector3.zero; car.position = Spawn; car.rotation = Quaternion.identity; Follow(true); }
        private void Close() { var callback = onClose; onClose = null; gameObject.SetActive(false); RestoreCameras(); callback?.Invoke(); Destroy(gameObject); }
        private void RestoreCameras() { foreach (var camera in previousCameras) if (camera != null) camera.enabled = true; previousCameras.Clear(); }
        private void OnDestroy() { RestoreCameras(); foreach (var material in materials) Destroy(material); }
    }

    public sealed class DriveHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action<bool> Changed;
        public void OnPointerDown(PointerEventData data) => Changed?.Invoke(true);
        public void OnPointerUp(PointerEventData data) => Changed?.Invoke(false);
        public void OnPointerExit(PointerEventData data) => Changed?.Invoke(false);
        private void OnDisable() => Changed?.Invoke(false);
    }
}
