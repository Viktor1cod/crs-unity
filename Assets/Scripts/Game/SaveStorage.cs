using System;
using CRS.Core.Economy;
using CRS.Core.Saves;
using UnityEngine;

namespace CRS.Game
{
    /// <summary>
    /// Keeps the saved game between launches. Uses PlayerPrefs (browser storage in Web builds).
    /// Platform cloud saves will plug in here later, the rest of the game does not care where the data lives.
    /// </summary>
    public static class SaveStorage
    {
        private const string Key = "crs.save";

        /// <summary>The saved game, or null when there is none or it cannot be read.</summary>
        public static GameSession Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(Key, string.Empty);
                if (string.IsNullOrEmpty(json)) return null;
                return GameSession.FromSave(JsonUtility.FromJson<SaveData>(json));
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveStorage: saved game ignored, starting a new one. " + e.Message);
                return null;
            }
        }

        public static void Save(GameSession session)
        {
            try
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(session.ToSave()));
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveStorage: could not save the game. " + e.Message);
            }
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
