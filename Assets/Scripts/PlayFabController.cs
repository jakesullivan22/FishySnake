using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayFabController : MonoBehaviour
{
    public static PlayFabController PFC;
    string email;
    string password;
    string username;
    string tempUsername = "Temp12345678922";
    int leaderboardIndex;
    public List<string> leaderboardNames;
    public List<int> highScores;
    public List<int> ranks;

    public GameObject leaderboardPrefab;
    public GameObject errorPopupPrefab;
    int playfabHighScore;
    int numLeaderboardEntries = 10;
    int playerRank = -1;

    public void Awake()
    {
        if (PFC == null)
        {
            PFC = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (PFC != this)
        {
            Destroy(gameObject);
        }

        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "6FACD"; 
        }
    }

    public void Login()
    {
        var request = new LoginWithEmailAddressRequest { Email = email, Password = password };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }

    public void TempLogin()
    {
        var request = new LoginWithCustomIDRequest { CustomId = tempUsername, CreateAccount = true };
        PlayFabClientAPI.LoginWithCustomID(request, OnTempLoginSuccess, OnTempLoginFailure);
    }

    void OnTempLoginSuccess(LoginResult result)
    {
        SetUsername(tempUsername);
        FindObjectOfType<MainMenu>()?.Play();
    } 

    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Login was successful");
        PlayerPrefs.SetString("Email", email);
        PlayerPrefs.SetString("Password", password);
        PlayerPrefs.SetInt("noLeaderboard", 0);

        FindObjectOfType<MainMenu>()?.OnLoginSuccess();
        GameCanvas gc = GameCanvas.gc;
        if (gc != null)
        {
            GetPlayFabHighScore();
        }
    }

    void OnLoginFailure(PlayFabError error)
    {
        var registerRequest = new RegisterPlayFabUserRequest { Email = email, Password = password, Username = username };
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, OnRegisterSuccess, OnRegisterFailure);
    }

    void OnTempLoginFailure(PlayFabError error)
    {
        FindObjectOfType<MainMenu>()?.Play();
    }

    void OnUsernameUpdateSuccess(UpdateUserTitleDisplayNameResult result)
    {
        Debug.Log(result.DisplayName + " is your new display name");
        OverlayCanvas.oc.textMessage.text = "Press Space to play again.";
    }

    void OnGeneralPlayfabFailure(PlayFabError error)
    {
        SpawnError(error.GenerateErrorReport());
    }

    void SpawnError(string _message)
    {
        int highestOrder = -1;
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas canvasInFront = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].sortingOrder > highestOrder)
            {
                highestOrder = canvases[i].sortingOrder;
                canvasInFront = canvases[i];
            }
        }
        GameObject errorPopup = Instantiate(errorPopupPrefab, canvasInFront.transform);
        errorPopup.GetComponentInChildren<Text>().text = _message;
        Debug.LogError(_message);
        Destroy(errorPopup, 7f);
    }

    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        Debug.Log("Register was successful");
        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.SetString("Email", email);
        PlayerPrefs.SetString("Password", password);
        FindObjectOfType<MainMenu>()?.OnLoginSuccess();
        PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest { DisplayName = username }, OnUsernameUpdateSuccess, OnGeneralPlayfabFailure);
        GameCanvas gc = GameCanvas.gc;
        if (gc != null)
        {
            SetHighScore(gc.GetCurScore());
        }
    }

    void OnRegisterFailure(PlayFabError error)
    {
        switch (error.GenerateErrorReport())
        {
            case "/Client/RegisterPlayFabUser: Email address not available\nEmail: Email address already exists. ":
                SpawnError("This email exists, and the password is incorrect.");
                break;
            case "/Client/RegisterPlayFabUser: Username not available":
                SpawnError("This username not available.");
                break;
            case "/Client/RegisterPlayFabUser: Invalid input parameters\nEmail: Email address is not valid.":
                SpawnError("Email address is not valid");
                break;
            case "/Client/RegisterPlayFabUser: Username not available\nUsername: User name already exists.":
                SpawnError("Username already exists");
                break;
            default:
                OnGeneralPlayfabFailure(error);
                break;
        }
        if (IsLoggedIn())
        {
            SetUsername(tempUsername);
        }
        else
        {
            SetUsername("");
        }
        SetUserPassword("");
        SetUserEmail("");
        FindObjectOfType<MainMenu>()?.OnLoginFailure();
    }

    public void SetUserEmail(string _email)
    {
        email = _email;
    }

    public void SetUserPassword(string _password)
    {
        password = _password;
    }

    public void SetUsername(string _username)
    {
        username = _username;
    }

    public bool IsLoggedIn()
    {
        return PlayFabClientAPI.IsClientLoggedIn();
    }

    public bool IsTempLogin()
    {
        return username == tempUsername;
    }

    public void SetHighScore(int _score)
    {
        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
            Statistics = new List<StatisticUpdate> { new StatisticUpdate { StatisticName = "highScore", Value = _score }, }
        },
        OnSetHighScoreSuccess,
        OnGeneralPlayfabFailure);
    }

    public void DeleteMenus()
    {
        Leaderboard leaderboard = FindObjectOfType<Leaderboard>();
        if (leaderboard != null)
        {
            Destroy(leaderboard.gameObject);
        }
    }

    public void OnSetHighScoreSuccess(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("User statistics updated");
        DeleteMenus();
    }

    public void GetPlayFabHighScore()
    {
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            OnGetHighScore,
            error => Debug.LogError(error.GenerateErrorReport())
        );
    }

    void OnGetHighScore(GetPlayerStatisticsResult result)
    {
        Debug.Log("Received the following Statistics:");
        foreach (var stat in result.Statistics)
        {
            switch (stat.StatisticName)
            {
                case "highScore":
                    playfabHighScore = stat.Value;
                    break;
                default:
                    break;
            }
            Debug.Log("Statistic (" + stat.StatisticName + "): " + stat.Value);
        }

        GameCanvas.gc?.GameOverPart2();
    }

    public int GetLocalHighScore()
    {
        return playfabHighScore;
    }

    public int GetPlayerRank()
    {
        return playerRank;
    }

    public void GetLeaderboardAroundPlayer()
    {
        var requestProximityLeaderboard = new GetLeaderboardAroundPlayerRequest { StatisticName = "highScore", MaxResultsCount = numLeaderboardEntries };
        PlayFabClientAPI.GetLeaderboardAroundPlayer(requestProximityLeaderboard, OnGetLeaderboardProximitySuccess, OnGeneralPlayfabFailure);
    }

    public void GetLeaderboardStartingAtIndex(int _start)
    {
        var requestLeaderboard = new GetLeaderboardRequest {StartPosition = _start , StatisticName = "highScore", MaxResultsCount = 100 };
        PlayFabClientAPI.GetLeaderboard(requestLeaderboard, OnGetLeaderboardSuccess, OnGetLeaderboardAtIndexFailure);
    }

    public void ResetLeaderboard()
    {
        leaderboardNames = new List<string>();
        highScores = new List<int>();
        ranks = new List<int>();
        leaderboardIndex = 0;
    }

    void OnGetLeaderboardProximitySuccess(GetLeaderboardAroundPlayerResult result)
    {
        foreach (PlayerLeaderboardEntry player in result.Leaderboard)
        {
            if (player.StatValue == 0)
            {
                continue;
            }

            Debug.Log(player.DisplayName + ": " + player.StatValue);
            leaderboardNames.Add(player.DisplayName);
            highScores.Add(player.StatValue);
            ranks.Add(player.Position + 1);

            if (player.DisplayName == PlayerPrefs.GetString("Username", "???"))
            {
                playerRank = player.Position + 1;
            }
        }

        Instantiate(leaderboardPrefab, FindObjectOfType<MainMenu>().transform);
    }

    void OnGetLeaderboardSuccess(GetLeaderboardResult result)
    {
        foreach (PlayerLeaderboardEntry player in result.Leaderboard)
        {
            if (player.StatValue == 0)
            {
                continue;
            }

            Debug.Log(player.DisplayName + ": " + player.StatValue);
            leaderboardNames.Add(player.DisplayName);
            highScores.Add(player.StatValue);
            ranks.Add(player.Position);
        }
        
        if (result.Leaderboard.Count == 100)
        {
            leaderboardIndex += 100;
            GetLeaderboardStartingAtIndex(leaderboardIndex);
        }
        else
        {
            Instantiate(leaderboardPrefab, OverlayCanvas.oc.textMessage.transform.parent);
        }
    }

    void OnGetLeaderboardAtIndexFailure(PlayFabError error)
    {
        OnGeneralPlayfabFailure(error);
        Instantiate(leaderboardPrefab, OverlayCanvas.oc.textMessage.transform.parent);
    }
}
