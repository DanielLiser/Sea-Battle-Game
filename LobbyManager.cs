using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public Button findGameButton;
    private const string ServerIP= "127.0.0.1";

    public GameObject MainScreen;      
    public GameObject searchingScreen; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MainScreen.SetActive(true);
        searchingScreen.SetActive(false);
        findGameButton.onClick.AddListener(onFindGameClicked);

    }
    void Update()
    {
        if (NetworkManager.Instance.lastRecivedData != null)
        {
            GameData data = NetworkManager.Instance.lastRecivedData;
            if (data.type == "GAME START")
            {
                NetworkManager.Instance.lastRecivedData = null;
                NetworkManager.Instance.myPlayerIndex = data.symbol;
                //PlayerPrefs.SetInt("MyPlayerIndex", data.symbol);
                SceneManager.LoadScene("GameScene");
            }
        }
    }

    void onFindGameClicked()
    {
        Debug.Log("1. Button Clicked - Starting process...");
        MainScreen.SetActive(false);
        searchingScreen.SetActive(true);

        // בדיקה שהמנהל קיים
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("CRITICAL ERROR: NetworkManager is missing!");
            return;
        }

        Debug.Log("2. Trying to connect to " + ServerIP + "...");
        NetworkManager.Instance.ConnectToServer(ServerIP);

        Debug.Log("3. Connection passed (or failed silently). Sending Data...");

        GameData request = new GameData("FIND_GAME", "Player");
        NetworkManager.Instance.SendJson(request);

        Debug.Log("4. Data Sent!");

        findGameButton.interactable = false;
    }
}
