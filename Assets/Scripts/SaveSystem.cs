using System.IO;
using UnityEngine;

public class SaveSystem
{
    private static string savePath => Application.persistentDataPath + "/save.json";

    public static void SavePlayerData(PlayerData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    public static PlayerData LoadPlayerData()
    {
        if (!File.Exists(savePath))
            return null; // boþ veri ile baþla

        string json = File.ReadAllText(savePath);
        return JsonUtility.FromJson<PlayerData>(json);
    }
}
