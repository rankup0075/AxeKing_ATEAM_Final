// GameOverUI.cs
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    public void OnClickRestart()
    {
        GameManager.Instance.RestartFromGameOver();
    }

    public void OnClickToMainMenu()
    {
        UIManager.Instance.ReturnToMainMenu();
    }

    public void OnClickQuit()
    {
        UIManager.Instance.QuitGame();
    }
}
