using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class RoomInfoDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text RoomNameInput;
    [SerializeField] private TMP_Text PlayerNumberInput;
    [SerializeField] private Image LockIcon;

    public GameObject passwordPopup;
    public RoomSelectScript roomSelectScript;

    private int id;
    private bool usesPassword;

    public void UpdateRoomInfo(JSONRoomInfo roomInfo)
    {
        RoomNameInput.text = roomInfo.name;
        PlayerNumberInput.text = roomInfo.players.Count.ToString();
        if (roomInfo.usesPassword)
        {
            LockIcon.enabled = true;
            usesPassword = true;
        }
        else
        {
            LockIcon.enabled = false;
            usesPassword = false;
        }
        id = roomInfo.id;
    }

    IEnumerator JoinRoomCoroutine()
    {
        Debug.Log(GameManager.user.id);
        Debug.Log(id);

        if (usesPassword)
        {
            passwordPopup.SetActive(true);
            PasswordPopupScript popupScript = passwordPopup.GetComponent<PasswordPopupScript>();
            yield return new WaitUntil(() => popupScript.submitted == true);
            string password = popupScript.passwordInputText;
            popupScript.submitted = false;
            popupScript.passwordInputText = "";
            passwordPopup.SetActive(false);

            Debug.Log("Password: " + password);

            string url = $"{GameManager.serverRoomsURL}/joinRoom/{GameManager.user.id}/{id}/{password}";
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
                    Debug.Log("Joined room successfully");
                    GameManager.roomInfo = resp.room;
                    GameManager.ChangeGameState(GameManager.GameState.IN_ROOM);
                    roomSelectScript.suspendRefresh = false;
                }
                else
                {
                    Debug.Log("Failed to join room");
                    roomSelectScript.suspendRefresh = false;
                }
            }
        }
        else
        {
            string url = $"{GameManager.serverRoomsURL}/joinRoom/{GameManager.user.id}/{id}";
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
                    Debug.Log("Joined room successfully");
                    GameManager.roomInfo = resp.room;
                    GameManager.ChangeGameState(GameManager.GameState.IN_ROOM);
                    roomSelectScript.suspendRefresh = false;
                }
                else
                {
                    Debug.Log("Failed to join room");
                    roomSelectScript.suspendRefresh = false;
                }
            }
        }
    }

    public void OnClick()
    {
        StartCoroutine(JoinRoomCoroutine());
        roomSelectScript.suspendRefresh = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (passwordPopup.activeSelf)
            {
                passwordPopup.SetActive(false);
                StopAllCoroutines();
                roomSelectScript.suspendRefresh = false;
            }
        }
    }
}
