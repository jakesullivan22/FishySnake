using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    int gameSceneIndex = 1;
    PlayFabController PFC;
    public GameObject mainMenuPanel;

    void Start()
    {
        PFC = PlayFabController.PFC;

        //check to see if player has a userID
        if (PlayerPrefs.HasKey("Email"))
        {
            //if so then login and show leaderboard button if successful
            PFC.SetUserEmail(PlayerPrefs.GetString("Email"));
            PFC.SetUserPassword(PlayerPrefs.GetString("Password"));
            PFC.SetUsername(PlayerPrefs.GetString("Username"));
            PFC.Login();
        }
        else
        {
            //if not then just open the game scene
            PFC.TempLogin();
            //once they die, then open a leaderboard for them to enter their username and register account info
        }
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneIndex);
    }

    public void ViewLeaderboard()
    {
        PFC.ResetLeaderboard();
        PFC.GetLeaderboardAroundPlayer();
    }

    public void Logout()
    {
        PFC.TempLogin();
    }

    public void OnLoginSuccess()
    {
        mainMenuPanel.SetActive(true);
    }

    public void OnLoginFailure()
    {
        PFC.TempLogin();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
