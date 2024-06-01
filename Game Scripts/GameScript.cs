using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using System.Security.Cryptography;
using UnityEngine.Networking;
using System;

public class GameScript : MonoBehaviour
{
    public GameObject FirstTimeObj;
    public GameObject OtherTimesObj;
    public TMP_InputField inputField;
    public Button submitButton;
    public GameObject PlayerDonePrefab;
    public List<PlayerDoneScript> players = new List<PlayerDoneScript>();
    public GameObject playersParent;
    public TMP_Text lastText;
    private int currentRound = 0;

    IEnumerator GetRoomInfo(Action<JSONRoomInfo> callback)
    {
        string url = $"{GameManager.serverRoomsURL}/getRoom/{GameManager.roomInfo.id}";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            Debug.Log(www.downloadHandler.text);
            JSONRoomResponse roomInfo = JSONRoomResponse.CreateFromJSON(www.downloadHandler.text);
            if (roomInfo.success)
            {
                callback(roomInfo.room);
            }
            else
            {
                Debug.LogError("Failed to get room info");
            }
        }
    }

    IEnumerator GetUserInfo(Action<User> callback, int playerID)
    {
        string url = $"{GameManager.serverURL}/user/getUserByID/{playerID}";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            JSONUserInfo userInfo = JSONUserInfo.CreateFromJSON(www.downloadHandler.text);
            User user = User.FromJsonUserInfo(userInfo);
            callback(user);
        }
    }

    IEnumerator PopulatePlayers()
    {
        yield return GetRoomInfo((roomInfo) =>
        {
            int i = 0;
            foreach (int userID in roomInfo.players)
            {
                if (userID == GameManager.user.id && GameManager.user.id != User.AnonymousUser.id)
                {
                    // remove the user from the ready list
                    roomInfo.readyPlayers.RemoveAt(i);
                    continue;
                }

                if (GameManager.user.id == User.AnonymousUser.id && i == GameManager.anonymousUserPosition)
                {
                    roomInfo.readyPlayers.RemoveAt(i);
                    continue;
                }

                if (userID == User.AnonymousUser.id)
                {
                    GameObject playerDone = Instantiate(PlayerDonePrefab, playersParent.transform);
                    PlayerDoneScript playerDoneScript = playerDone.GetComponent<PlayerDoneScript>();
                    playerDoneScript.SetUser(User.AnonymousUser);
                    players.Add(playerDoneScript);
                    continue;
                }


                StartCoroutine(GetUserInfo((user) =>
                {
                    GameObject playerDone = Instantiate(PlayerDonePrefab, playersParent.transform);
                    PlayerDoneScript playerDoneScript = playerDone.GetComponent<PlayerDoneScript>();
                    playerDoneScript.SetUser(user);
                    players.Add(playerDoneScript);
                }, userID));

                i++;
            }
        });
    }

    IEnumerator UpdatePlayersReady()
    {
        yield return GetRoomInfo((roomInfo) =>
        {
            GameManager.roomInfo = roomInfo;
            int i = 0;
            Debug.Log(players.Count);
            foreach (bool ready in roomInfo.game.donePlayers)
            {
                int userID = roomInfo.players[i];
                if (userID == GameManager.user.id && GameManager.user.id != User.AnonymousUser.id)
                {
                    continue;
                }

                if (GameManager.user.id == User.AnonymousUser.id && i == GameManager.anonymousUserPosition)
                {
                    continue;
                }

                if (i > players.Count - 1)
                {
                    break;
                }

                players[i].updateIndicator(ready);

                i++;
            }
        });
    }

    IEnumerator GameLoop()
    {
        while (true)
        {
            yield return UpdatePlayersReady();
            if (GameManager.roomInfo.game.done)
            {
                GameManager.ChangeGameState(GameManager.GameState.GAME_OVER);
                break;
            }

            if (GameManager.roomInfo.game.roundNumber != currentRound)
            {
                currentRound = GameManager.roomInfo.game.roundNumber;
                GetLastSentence();
                FirstTimeObj.SetActive(false);
                OtherTimesObj.SetActive(true);

            }

            yield return new WaitForSeconds(1);
        }
    }

    IEnumerator Submit()
    {
        string url = $"{GameManager.serverRoomsURL}/addSentence/{GameManager.user.id}/{GameManager.roomInfo.id}/{inputField.text}";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            JSONRoomResponse roomInfo = JSONRoomResponse.CreateFromJSON(www.downloadHandler.text);
            GameManager.roomInfo = roomInfo.room;

            inputField.interactable = false;
            submitButton.interactable = false;
        }
    }

    public void OnSubmit()
    {
        StartCoroutine(Submit());
    }

    void GetLastSentence()
    {
        foreach (string sentence in GameManager.roomInfo.game.sentences)
        {
            Debug.Log(sentence);
        }

        if (GameManager.anonymousUserPosition != -1)
        {
            string sentence = GameManager.roomInfo.game.sentences[GameManager.anonymousUserPosition];
            // get last 3 words
            string[] words = sentence.Split(' ');
            if (words.Length < 3)
            {
                lastText.text = sentence;
            }
            else
            {
                lastText.text = "..." + words[^3] + " " + words[^2] + " " + words[^1];
            }
        }
        else
        {
            // find user index in room
            int i = 0;
            foreach (int userID in GameManager.roomInfo.players)
            {
                if (userID == GameManager.user.id)
                {
                    string sentence = GameManager.roomInfo.game.sentences[i];

                    string[] words = sentence.Split(' ');
                    if (words.Length < 3)
                    {
                        lastText.text = sentence;
                    }
                    else
                    {
                        lastText.text = "..." + words[^3] + " " + words[^2] + " " + words[^1];
                    }

                    break;
                }
                i++;
            }
        }

        inputField.interactable = true;
        submitButton.interactable = true;
        inputField.text = "";
    }

    void OnEnable()
    {
        StartCoroutine(PopulatePlayers());
        StartCoroutine(GameLoop());
        GetLastSentence();
        FirstTimeObj.SetActive(true);
        OtherTimesObj.SetActive(false);
        currentRound = 0;
    }
}
