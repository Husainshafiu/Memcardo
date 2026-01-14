using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    void Start()
    {
        
    }

    public void LoadGame()
    {
        if (SceneManager.GetSceneByName("IngameScene").IsValid() || Application.CanStreamedLevelBeLoaded("IngameScene"))
        {
            SceneManager.LoadScene("IngameScene");
        }
        else
        {
            Debug.LogError("Scene 'IngameScene' not found in Build Settings!");
        }
    }

    public void ExitGame()
    {
        Debug.Log("ExitGame called!");
        Application.Quit();
    }
}
