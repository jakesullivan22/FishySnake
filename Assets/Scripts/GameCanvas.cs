using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameCanvas : MonoBehaviour
{
    public static GameCanvas gc;

    PlayFabController PFC;
    GameManager gm;

    bool doneOuroboros;
    Coroutine couroboros;
    public Image ouroboros;

    int curScore;
    bool isGameOver;

    void Awake()
    {
        gc = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        PFC = PlayFabController.PFC;
        gm = GameManager.gm;
        PlayerController.RegisterOnEat(AddScore);
        PlayerController.RegisterOnDie(GameOver);
        PlayerController.RegisterOnGrow(DoneOuroboros);
        PlayerController.RegisterOnSetTail(DoOuroboros);
    }

    void AddScore(int _change)
    {
        curScore += _change;
    }

    void AddScore(Fish _fish)
    {
        AddScore(_fish.GetPoints());
    }

    IEnumerator Ouroboros()
    {
        float timer = 0f;
        float cycleTimer = 5f;
        bool isIncreasing = true;
        float halfCycle = cycleTimer / 2f;
        while (doneOuroboros == false)
        {
            yield return null;
            timer += isIncreasing ? Time.deltaTime : -Time.deltaTime;
            ouroboros.color = Color.Lerp(Color.clear, Color.green, timer / cycleTimer / 2f);

            if (Mathf.Abs(timer - halfCycle) > halfCycle)
            {
                isIncreasing = isIncreasing == false;
            }
        }

        while (ouroboros.color != Color.clear)
        {
            yield return null;
            timer -= Time.deltaTime;
            ouroboros.color = Color.Lerp(Color.clear, Color.green, timer / cycleTimer / 2f);
        }
    }

    public void GameOver()
    {
        isGameOver = true;

        if (PlayerPrefs.GetInt("noLeaderboard", 0) == 1)
        {
            return;
        }

        if (PFC.IsLoggedIn() == false || curScore < 10)
        {
            return;
        }

        if (PFC.IsTempLogin())
        {
            PFC.ResetLeaderboard();
            PFC.GetLeaderboardStartingAtIndex(0);
            return;
        }

        //if curscore is higher than playfab highscore, then save it
        PFC.GetPlayFabHighScore();
    }

    public void GameOverPart2()
    {
        if (PFC.GetLocalHighScore() < curScore)
        {
            PFC.SetHighScore(curScore);
        }

        OverlayCanvas oc = OverlayCanvas.oc;
        if (oc != null)
        {
            oc.MainMenuButtonCheck();
        }
        PFC.DeleteMenus();
    }

    public int GetCurScore()
    {
        return curScore;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    public void Retry()
    {
        SceneManager.LoadScene(1);
    }

    void DoOuroboros()
    {
        if (couroboros == null)
        {
            couroboros = StartCoroutine(Ouroboros());
        }
    }

    void DoneOuroboros(float _float)
    {
        doneOuroboros = true;
    }

    void OnDisable()
    {
        PlayerController.UnregisterOnDie(GameOver);
        PlayerController.UnregisterOnEat(AddScore);
        PlayerController.UnregisterOnGrow(DoneOuroboros);
        PlayerController.UnregisterOnSetTail(DoOuroboros);
    }
}
