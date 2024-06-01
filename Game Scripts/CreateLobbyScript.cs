using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using UnityEngine.Networking;
using System;

public class CreateLobbyScript : MonoBehaviour
{
    public TMP_InputField lobbyName;
    public TMP_InputField password;
    public TMP_InputField maxRound;

    public Button CreateButton;

    IEnumerator CreateLobby()
    {
        CreateButton.interactable = false;
        string url;
        if (password.text == "")
            url = $"{GameManager.serverRoomsURL}/createRoom/{GameManager.user.id}/{lobbyName.text}/{maxRound.text}";
        else
            url = $"{GameManager.serverRoomsURL}/createRoom/{GameManager.user.id}/{lobbyName.text}/{maxRound.text}/{password.text}";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(webRequest.error);
            CreateButton.interactable = true;
        }
        else
        {
            string response = webRequest.downloadHandler.text;
            Debug.Log(response);
            JSONRoomResponse roomInfo = JSONRoomResponse.CreateFromJSON(webRequest.downloadHandler.text);
            CreateButton.interactable = true;
            if (roomInfo.success)
            {
                GameManager.roomInfo = roomInfo.room;
                Debug.Log("Room created successfully");
                GameManager.ChangeGameState(GameManager.GameState.IN_ROOM);
            }
            else
            {
                Debug.LogError("Room creation failed");
            }
        }
    }

    public void OnPressCreate()
    {
        StartCoroutine(CreateLobby());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.ChangeGameState(GameManager.GameState.BROWSING);
        }

        if (GameManager.user.premium)
        {
            maxRound.interactable = true;
        }
        else
        {
            maxRound.interactable = false;
        }
    }
}
