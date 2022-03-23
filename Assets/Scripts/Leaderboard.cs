using UnityEngine;
using UnityEngine.UI;

public class Leaderboard : MonoBehaviour
{
    PlayFabController PFC;
    OverlayCanvas oc;
    GameCanvas gc;

    public GameObject leaderBoardTextPrefab;
    public GameObject nameInputPrefab;
    public GameObject rankContainer;
    public GameObject nameContainer;
    public GameObject scoreContainer;
    
    public Button submitButton;
    public Button tryAgainButton;
    public Button noThanksButton;
    public Button closeButton;
    
    InputField usernameInput;

    // Start is called before the first frame update
    void Start()
    {
        PFC = PlayFabController.PFC;
        oc = OverlayCanvas.oc;
        gc = GameCanvas.gc;
        if (oc != null)
        {
            oc.textMessage.text = "";
        }
        
        if (PFC.IsTempLogin())
        {
            TempLoginSetup();
        }
        else if (PFC.IsLoggedIn())
        {
            PopulateTable(0, PFC.highScores.Count, PFC.GetPlayerRank(), false);
            submitButton.gameObject.SetActive(false);
            tryAgainButton.gameObject.SetActive(false);
            noThanksButton.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void TempLoginSetup()
    {
        int score = gc.GetCurScore();
        int playerRank = 0;

        for (int i = 0; i < PFC.highScores.Count; i++)
        {
            if (score >= PFC.highScores[i])
            {
                playerRank = i;
                break;
            }
        }

        if (playerRank == 0)
        {
            playerRank = PFC.highScores.Count;
        }

        PFC.highScores.Insert(playerRank, score);
        PFC.leaderboardNames.Insert(playerRank, "");
        PFC.ranks.Add(PFC.ranks.Count);

        int populateStart;
        int populateTerminator;
        
        if (PFC.highScores.Count < 10)
        {
            populateStart = 0;
            populateTerminator = PFC.highScores.Count;
        } 
        else if (playerRank < 5)
        {
            populateStart = 0;
            populateTerminator = 10;
        }
        else if (playerRank > PFC.highScores.Count - 5)
        {
            populateStart = PFC.highScores.Count - 10;
            populateTerminator = PFC.highScores.Count;
        }
        else
        {
            populateStart = playerRank - 5;
            populateTerminator = playerRank + 5;
        }

        PopulateTable(populateStart, populateTerminator, playerRank, true);
    }

    void PopulateTable(int _start, int _stop, int _playerRank, bool _needNameInput)
    {
        for (int i = _start; i < _stop; i++)
        {
            Text rankText;
            Text usernameText = null;
            Text scoreText;

            if (i == _playerRank && _needNameInput)
            {
                usernameInput = Instantiate(nameInputPrefab, nameContainer.transform).GetComponent<InputField>();
            }
            else
            {
                usernameText = Instantiate(leaderBoardTextPrefab, nameContainer.transform).GetComponent<Text>();
                usernameText.text = PFC.leaderboardNames[i];
            }

            string rank = PFC.ranks[i].ToString();
            if (rank.Length == 1 || rank[rank.Length - 2] != '1')
            {
                switch (rank[rank.Length - 1])
                {
                    case '1':
                        rank += "st";
                        break;
                    case '2':
                        rank += "nd";
                        break;
                    case '3':
                        rank += "rd";
                        break;
                    default:
                        rank += "th";
                        break;
                }
            }
            else
            {
                rank += "th";
            }

            rankText = Instantiate(leaderBoardTextPrefab, rankContainer.transform).GetComponent<Text>();
            rankText.text = rank;

            scoreText = Instantiate(leaderBoardTextPrefab, scoreContainer.transform).GetComponent<Text>();
            scoreText.text = PFC.highScores[i].ToString();

            if (PFC.ranks[i] == _playerRank)
            {
                Color darkRed = Color.Lerp(Color.black, Color.red, .5f);
                rankText.color = darkRed;
                scoreText.color = darkRed;

                if (_needNameInput == false)
                {
                    usernameText.color = darkRed;
                }
            }
        }
    }

    public void NoThanksDontAskAgain()
    {
        PlayerPrefs.SetInt("noLeaderboard", 1);
        oc.participateInLeaderboardButton.gameObject.SetActive(true);
        Close();
    }

    public void Close()
    {
        Destroy(gameObject);
    }

    public void Submit()
    {
        PFC.SetUsername(usernameInput.text);
        int numAccounts = PFC.leaderboardNames.Count;
        PFC.SetUserEmail("autoEmail" + (numAccounts + 1) + "@playfab.com");
        PFC.SetUserPassword("autoPassword" + (numAccounts + 1));
        PFC.Login();
    }

    public void TryAgain()
    {
        gc.Retry();
    }

    private void Update()
    {
        if (gc != null)
        {
            submitButton.interactable = usernameInput.text != "";
        }
    }
}