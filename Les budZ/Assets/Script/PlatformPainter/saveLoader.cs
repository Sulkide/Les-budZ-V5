using UnityEngine;
using System.IO;

public static class SaveLoader
{
    public static GameManager.GameData LoadGameData(int slot)
    {
        string path = Path.Combine(Application.persistentDataPath, $"slot{slot}.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveLoader] Aucun fichier de sauvegarde en '{path}'");
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<GameManager.GameData>(json);
    }
}