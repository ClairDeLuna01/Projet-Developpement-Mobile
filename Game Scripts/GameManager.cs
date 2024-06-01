using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Defective.JSON;
using UnityEngine.UI;

[System.Serializable]
public class User
{
    public int id;
    public string username;
    public string email;
    public string passwordHash;
    public Texture2D profilePicture;
    public Sprite profilePictureSprite;
    public bool premium;

    public static User AnonymousUser;

    public User(int id, string username, string email, string passwordHash, string profilePictureB64, bool premium)
    {
        this.id = id;
        this.username = username;
        this.email = email;
        this.passwordHash = passwordHash;
        this.premium = premium;

        byte[] profilePicBytes = Convert.FromBase64String(profilePictureB64);

        profilePicture = new Texture2D(1, 1);
        profilePicture.LoadImage(profilePicBytes);

        Rect rec = new Rect(0, 0, profilePicture.width, profilePicture.height);
        profilePictureSprite = Sprite.Create(profilePicture, rec, new Vector2(0.5f, 0.5f), 100);
    }

    public User(int id, string username, string email, string passwordHash, Texture2D profilePicture, bool premium)
    {
        this.id = id;
        this.username = username;
        this.email = email;
        this.passwordHash = passwordHash;
        this.premium = premium;

        this.profilePicture = profilePicture;

        Rect rec = new Rect(0, 0, profilePicture.width, profilePicture.height);
        profilePictureSprite = Sprite.Create(profilePicture, rec, new Vector2(0.5f, 0.5f), 100);
    }

    public User(int id, string username, string email, string passwordHash, bool premium)
    {
        this.id = id;
        this.username = username;
        this.email = email;
        this.passwordHash = passwordHash;
        this.premium = premium;

        profilePicture = Resources.Load<Texture2D>("defaultPP");

        Rect rec = new Rect(0, 0, profilePicture.width, profilePicture.height);
        profilePictureSprite = Sprite.Create(profilePicture, rec, new Vector2(0.5f, 0.5f), 100);
    }

    public static User FromJsonUserInfo(JSONUserInfo info)
    {
        if (info.profilePictureB64 == null)
            return new User(info.id, info.username, info.email, info.passwordHash, info.premium);
        else
            return new User(info.id, info.username, info.email, info.passwordHash, info.profilePictureB64, info.premium);
    }
}


public class GameManager : MonoBehaviour
{
    public static string serverURL = "https://clairdeluna.pythonanywhere.com";
    public static string serverRoomsURL = $"{serverURL}/rooms";


    public static User user = User.AnonymousUser;
    static public JSONRoomInfo roomInfo;

    public enum GameState
    {
        BROWSING,
        CREATE_ROOM,
        IN_ROOM,
        IN_GAME,
        GAME_OVER,
        LOGIN,
        REGISTER
    }
    static GameState state;

    public static GameObject browsePanel;
    public static GameObject createRoomPanel;
    public static GameObject roomPanel;
    public static GameObject gamePanel;
    public static GameObject gameOverPanel;
    public static GameObject loginPanel;
    public static GameObject registerPanel;

    public static GameManager instance;

    public GameObject browsePanelObj;
    public GameObject createRoomPanelObj;
    public GameObject roomPanelObj;
    public GameObject gamePanelObj;
    public GameObject gameOverPanelObj;
    public GameObject loginPanelObj;
    public GameObject registerPanelObj;

    public static Canvas canvas;
    public static RectTransform canvasRect;
    public static CanvasScaler canvasScaler;
    public Canvas canvasObj;

    public static int anonymousUserPosition = -1;

    public static void ChangeGameState(GameState newState)
    {
        Debug.Log("Changing game state");
        state = newState;
        browsePanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);

        switch (state)
        {
            case GameState.BROWSING:
                browsePanel.SetActive(true);
                break;
            case GameState.CREATE_ROOM:
                createRoomPanel.SetActive(true);
                break;
            case GameState.IN_ROOM:
                roomPanel.SetActive(true);
                break;
            case GameState.IN_GAME:
                gamePanel.SetActive(true);
                break;
            case GameState.GAME_OVER:
                gameOverPanel.SetActive(true);
                break;
            case GameState.LOGIN:
                loginPanel.SetActive(true);
                break;
            case GameState.REGISTER:
                registerPanel.SetActive(true);
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        browsePanel = browsePanelObj;
        createRoomPanel = createRoomPanelObj;
        roomPanel = roomPanelObj;
        gamePanel = gamePanelObj;
        gameOverPanel = gameOverPanelObj;
        loginPanel = loginPanelObj;
        registerPanel = registerPanelObj;
        canvas = canvasObj;
        canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasScaler = canvasObj.GetComponent<CanvasScaler>();

        user = User.AnonymousUser;

        ChangeGameState(GameState.BROWSING);
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        User.AnonymousUser = new User(0, "Anonymous", "Anonymous", "Anonymous", false);
    }
}
