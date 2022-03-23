using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class OverlayCanvas : MonoBehaviour
{
    public static OverlayCanvas oc;

    public Text textMessage;
    public Text curScoreText;
    public Text highScoreText;
    
    public GameObject participateInLeaderboardButton;
    public GameObject mainMenuButton;
    
    public GameObject musicX; //crossout to show music is turned off
    public GameObject soundX;
    
    PlayFabController PFC;
    AudioManager am;
    GameManager gm; 
    GameCanvas gc;

    private void Awake()
    {
        oc = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        gc = GameCanvas.gc;
        am = AudioManager.am;
        gm = GameManager.gm;
        PFC = PlayFabController.PFC;

        textMessage.text = "Press WASD to move.";
        
        participateInLeaderboardButton.gameObject.SetActive(PlayerPrefs.GetInt("noLeaderboard", 0) == 1 && PFC.IsTempLogin());
        MainMenuButtonCheck();
        SetScores(null);
        PlayerController.RegisterOnDoneEating(SetScores);
        musicX.SetActive(am.musicSource.enabled == false);
        soundX.SetActive(am.soundSource.enabled == false);
    }

    void SetScores(Fish _fish)
    {
        int score = gc.GetCurScore();

        curScoreText.text = score.ToString();
        
        if (PlayerPrefs.GetInt("HighScore", 0) < score)
        {
            PlayerPrefs.SetInt("HighScore", score);
        }
        highScoreText.text = "High Score: " + PlayerPrefs.GetInt("HighScore", 0);
    }

    public void MainMenuButtonCheck()
    {
        mainMenuButton.gameObject.SetActive(PFC.IsLoggedIn() && PFC.IsTempLogin() == false);
    }

    public void ToggleMusic()
    {
        am.musicSource.enabled = am.musicSource.enabled == false;
        musicX.SetActive(am.musicSource.enabled == false);
    }

    public void ToggleSound()
    {
        am.soundSource.enabled = am.soundSource.enabled == false;
        soundX.SetActive(am.soundSource.enabled == false);
    }

    public void OpenMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void ExitGame()
    {
        gm.ExitGame();
    }

    public void ParticipateInLeaderboard()
    {
        PlayerPrefs.SetInt("noLeaderboard", 0);
        if (gc.IsGameOver())
        {
            textMessage.text = "";
            gc.GameOver();
        }
        participateInLeaderboardButton.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        PlayerController.UnregisterOnDoneEating(SetScores);
    }

    void Update()
    {
        if (gc.IsGameOver() == false || FindObjectOfType<Leaderboard>() != null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            gc.Retry();
        }
    }
}
