using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class CardSaveData
{
    public string guid;
    public int textureIndex;
    public Color color;
    public Vector3 position;
    public bool isCompleted;
    public bool isFaceUp;
}

[System.Serializable]
public class GameSaveData
{
    public int score;
    public int gridX;
    public int gridY;
    public float cardSpacing;
    public List<CardSaveData> cards = new List<CardSaveData>();
    public int currentMatchedPairs;
}

public static class SaveManager
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "gamesave.json");

    public static void SaveGame(GameSaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    public static GameSaveData LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("No save file found.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log($"Game loaded from: {SavePath}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return null;
        }
    }

    public static bool SaveExists()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Save file deleted.");
        }
    }
}
