using System;
using System.IO;
using System.Reflection;
using CRS.Game.UI;
using CRS.Game.World;
using UnityEditor;
using UnityEngine;

namespace CRS.Editor
{
    public static class WorldPrototypeCheck
    {
        private static WorldPrototype world;
        private static MarketScreen screen;
        private static Rigidbody car;
        private static Vector3 start;
        private static double started;
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("CRS/Check district in Play Mode _F8")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode before checking district."); return; }
            try
            {
                screen = UnityEngine.Object.FindAnyObjectByType<MarketScreen>();
                if (screen == null) throw new Exception("Trade screen missing");
                typeof(MarketScreen).GetMethod("OpenDistrict", Flags).Invoke(screen, null);
                world = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
                car = (Rigidbody)typeof(WorldPrototype).GetField("car", Flags).GetValue(world);
                start = car.position;
                typeof(WorldPrototype).GetField("forward", Flags).SetValue(world, true);
                started = EditorApplication.timeSinceStartup;
                EditorApplication.update += Tick;
            }
            catch (Exception e) { Finish("FAIL: " + e); }
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup - started < 2) return;
            try
            {
                if (world == null || car == null) throw new Exception("District missing");
                float distance = Vector3.Distance(start, car.position);
                if (distance < 1) throw new Exception("Car did not move: " + distance);
                typeof(WorldPrototype).GetMethod("ResetCar", Flags).Invoke(world, null);
                if (Vector3.Distance(start, car.position) > .1f) throw new Exception("Reset failed");
                typeof(WorldPrototype).GetMethod("Close", Flags).Invoke(world, null);
                var canvas = (Transform)typeof(MarketScreen).GetField("canvasRoot", Flags).GetValue(screen);
                if (!screen.enabled || !canvas.gameObject.activeSelf) throw new Exception("Trade screen was not restored");
                Finish("PASS: district opened; car moved " + distance.ToString("F1") + " m; reset returned to spawn; trade screen restored.");
            }
            catch (Exception e) { Finish("FAIL: " + e); }
        }

        private static void Finish(string result)
        {
            EditorApplication.update -= Tick;
            Directory.CreateDirectory("Builds");
            File.WriteAllText("Builds/WorldCheck.txt", result);
            Debug.Log(result);
        }
    }
}
