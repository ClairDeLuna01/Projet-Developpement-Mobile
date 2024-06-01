using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using UnityEngine.Networking;
using System;

public class LobbyScript : MonoBehaviour
{
    public GameObject lobbyUserPrefab;
    public GameObject lobbyUserClickablePrefab;
    public GameObject lobbyUsersParent;

    private List<User> lobbyUsers = new List<User>();
    List<bool> lobbyUserReadyList;

    bool isUserReady = false;

    private Toggle readyToggle;
    public TMP_Text RoomMaxRoundDisplay;

    public void OnToggleReady()
    {
        isUserReady = readyToggle.isOn;
        StartCoroutine(ToggleUserReady());
    }

    void PopulateLobbyUsers()
    {
        // clear the lobby users
        foreach (Transform child in lobbyUsersParent.transform)
        {
            Destroy(child.gameObject);
        }

        // start by creating the user (clickable)
        GameObject host = Instantiate(lobbyUserClickablePrefab, lobbyUsersParent.transform);
        LobbyPlayerScript script = host.GetComponent<LobbyPlayerScript>();
        script.SetUser(GameManager.user, isUserReady);
        readyToggle = script.readyToggle;
        readyToggle.onValueChanged.AddListener(delegate { OnToggleReady(); });


        // then create the other users
        float cumHeight = host.GetComponent<RectTransform>().rect.height;
        for (int i = 0; i < lobbyUsers.Count; i++)
        {
            GameObject userObj = Instantiate(lobbyUserPrefab, lobbyUsersParent.transform);
            userObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -cumHeight);
            cumHeight += userObj.GetComponent<RectTransform>().rect.height;
            LobbyPlayerScript userScript = userObj.GetComponent<LobbyPlayerScript>();
            User user = lobbyUsers[i];
            userScript.SetUser(user, lobbyUserReadyList[i]);
        }
    }

    IEnumerator GetLobbyUsers()
    {
        string url = $"{GameManager.serverRoomsURL}/getRoom/{GameManager.roomInfo.id}";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            JSONRoomResponse resp = JSONRoomResponse.CreateFromJSON(webRequest.downloadHandler.text);
            GameManager.roomInfo = resp.room;
            if (resp.success)
            {
                if (resp.room.game == null)
                {

                    lobbyUsers = new List<User>();
                    List<int> lobbyUserIDList = resp.room.players;
                    lobbyUserReadyList = resp.room.readyPlayers;

                    int i = 0;

                    foreach (int userID in lobbyUserIDList)
                    {
                        if (userID == GameManager.user.id && GameManager.user.id != User.AnonymousUser.id)
                        {
                            // remove the user from the ready list
                            lobbyUserReadyList.RemoveAt(i);
                            continue;
                        }

                        // the current user is anonymous, and we don't want to show them twice in the lobby, also we want to keep track of which anonymous user is the current user
                        // to be able to update their ready status
                        if (GameManager.user.id == User.AnonymousUser.id && i == GameManager.anonymousUserPosition)
                        {
                            lobbyUserReadyList.RemoveAt(i);
                            continue;
                        }

                        if (userID == User.AnonymousUser.id)
                        {
                            lobbyUsers.Add(User.AnonymousUser);
                            continue;
                        }

                        string userURL = $"{GameManager.serverURL}/user/getUserByID/{userID}";
                        using UnityWebRequest userWebRequest = UnityWebRequest.Get(userURL);
                        yield return userWebRequest.SendWebRequest();

                        if (userWebRequest.result == UnityWebRequest.Result.ConnectionError)
                        {
                            Debug.Log("Error: " + userWebRequest.error);
                        }
                        else
                        {
                            Debug.Log("Received: " + userWebRequest.downloadHandler.text);
                            JSONUserInfo userResp = JSONUserInfo.CreateFromJSON(userWebRequest.downloadHandler.text);
                            if (userResp.success)
                            {
                                lobbyUsers.Add(User.FromJsonUserInfo(userResp));
                            }
                            else
                            {
                                Debug.Log("Failed to get user, things are going to break probably");
                            }
                        }

                        i++;
                    }

                    PopulateLobbyUsers();
                }
                else
                {
                    readyToggle.isOn = false;
                    GameManager.ChangeGameState(GameManager.GameState.IN_GAME);
                }
            }
            else
            {
                Debug.Log("Failed to get room"); // not ideal
            }
        }
    }

    IEnumerator ComputeAnonymousUserPos()
    {
        string url = $"{GameManager.serverRoomsURL}/getRoom/{GameManager.roomInfo.id}";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            JSONRoomResponse resp = JSONRoomResponse.CreateFromJSON(webRequest.downloadHandler.text);
            if (resp.success)
            {
                List<int> lobbyUserIDList = resp.room.players;
                int i = 0;
                GameManager.anonymousUserPosition = -1;

                foreach (int userID in lobbyUserIDList)
                {
                    if (userID == User.AnonymousUser.id)
                    {
                        GameManager.anonymousUserPosition = i;
                    }
                    i++;
                }

            }
            else
            {
                Debug.Log("Failed to get room");
            }
        }
    }

    IEnumerator ToggleUserReady()
    {
        if (GameManager.user.id != User.AnonymousUser.id)
        {
            string url = $"{GameManager.serverRoomsURL}/ready/{GameManager.user.id}/{GameManager.roomInfo.id}";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                Debug.Log("Received: " + webRequest.downloadHandler.text);
                JSONRoomResponse resp = JSONRoomResponse.CreateFromJSON(webRequest.downloadHandler.text);
                if (resp.success)
                {
                    Debug.Log("Toggled ready successfully");
                }
                else
                {
                    Debug.Log("Failed to toggle ready");
                }
            }
        }
        else
        {
            string url = $"{GameManager.serverRoomsURL}/readyAnonymous/{GameManager.roomInfo.id}/{GameManager.anonymousUserPosition}";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                Debug.Log("Received: " + webRequest.downloadHandler.text);
                JSONRoomResponse resp = JSONRoomResponse.CreateFromJSON(webRequest.downloadHandler.text);
                if (resp.success)
                {
                    Debug.Log("Toggled ready successfully");
                }
                else
                {
                    Debug.Log("Failed to toggle ready");
                }
            }
        }
    }


    // we have to do this synchronously because we need to leave the lobby before we destroy the object
    public static void LeaveLobby(bool ChangeGameState)
    {
        if (GameManager.roomInfo == null)
        {
            Debug.Log("No room to leave");
            if (ChangeGameState)
                GameManager.ChangeGameState(GameManager.GameState.BROWSING);
            return;
        }

        string url = $"{GameManager.serverRoomsURL}/leaveRoom/{GameManager.user.id}/{GameManager.roomInfo.id}";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SendWebRequest();

        while (!webRequest.isDone)
        {
            System.Threading.Thread.Sleep(100);
        }

        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            JSONRoomResponse resp = JSONRoomResponse.CreateFromJSON(webRequest.downloadHandler.text);
            if (resp.success)
            {
                Debug.Log("Left room successfully");
                GameManager.roomInfo = null;
                if (ChangeGameState)
                    GameManager.ChangeGameState(GameManager.GameState.BROWSING);
            }
            else
            {
                Debug.Log("Failed to leave room");
            }
        }
    }

    IEnumerator UpdateCoroutine()
    {
        while (true)
        {
            yield return GetLobbyUsers();
            yield return new WaitForSeconds(1);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            readyToggle.isOn = false;
            LeaveLobby(true);
        }
    }

    private void OnDestroy()
    {
        LeaveLobby(false);
    }

    void OnEnable()
    {
        StartCoroutine(UpdateCoroutine());
        StartCoroutine(ComputeAnonymousUserPos());

        RoomMaxRoundDisplay.text = $"{GameManager.roomInfo.roundMax}";
    }
}
