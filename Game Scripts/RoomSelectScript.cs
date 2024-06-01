using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using UnityEngine.Networking;
using System;

public class RoomSelectScript : MonoBehaviour
{
    public Button HostButton;
    public GameObject roomListItemPrefab;
    public GameObject passwordPopup;
    public List<JSONRoomInfo> roomsDisplay;
    public GameObject roomInfoDisplay;
    public TMP_Text bannerAccountName;
    public Image bannerAccountImage;
    private Image bannerAccountImageDefault;
    public bool suspendRefresh = false;

    IEnumerator GetRoomListCoroutine(Action<List<JSONRoomInfo>> callback)
    {
        string url = $"{GameManager.serverRoomsURL}/getRooms";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            JSONRoomListResponse resp = JSONRoomListResponse.CreateFromJSON(webRequest.downloadHandler.text);
            if (resp.success)
            {
                // Debug.Log("Rooms: " + resp.rooms);
                // Debug.Log("Rooms.size: " + resp.rooms.Count);
                callback(resp.rooms);
            }
            else
            {
                Debug.Log("Failed to get room list"); // 😰
            }
        }
    }

    IEnumerator UpdateRoomsDisplayCoroutine()
    {
        List<JSONRoomInfo> rooms = null;
        yield return GetRoomListCoroutine((roomList) => rooms = roomList);

        // Debug.Log("Rooms: " + rooms);
        // Debug.Log("Rooms.size: " + rooms.Count);
        roomsDisplay = rooms;

        if (rooms == null)
        {
            Debug.Log("No rooms found");
            yield break;
        }

        foreach (Transform child in roomInfoDisplay.transform)
        {
            if (child.CompareTag("RoomInfoPrefab"))
                Destroy(child.gameObject);
        }

        int i = 0;
        foreach (JSONRoomInfo room in rooms)
        {
            if (room.game != null)
                continue;
            // Debug.Log("Room: " + room.name);
            int offset = -i * 130;
            GameObject roomListItem = Instantiate(roomListItemPrefab, roomInfoDisplay.transform);
            RoomInfoDisplay roomInfoDisplayScript = roomListItem.GetComponent<RoomInfoDisplay>();
            roomInfoDisplayScript.UpdateRoomInfo(room);
            roomInfoDisplayScript.passwordPopup = passwordPopup;
            roomInfoDisplayScript.roomSelectScript = this;
            roomListItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, offset);

            i++;
        }
    }

    IEnumerator UpdateDisplayLoop()
    {
        while (true)
        {
            if (!suspendRefresh)
                StartCoroutine(UpdateRoomsDisplayCoroutine());
            yield return new WaitForSeconds(5);
        }
    }

    void OnEnable()
    {
        StartCoroutine(UpdateDisplayLoop());
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    public void OnHostButtonClicked()
    {
        GameManager.ChangeGameState(GameManager.GameState.CREATE_ROOM);
    }

    public void OnLoginButtonClicked()
    {
        GameManager.ChangeGameState(GameManager.GameState.LOGIN);
    }

    void Update()
    {
        if (GameManager.user.id != User.AnonymousUser.id)
        {
            bannerAccountName.text = GameManager.user.username;
            HostButton.interactable = true;
            bannerAccountImage.sprite = GameManager.user.profilePictureSprite;
        }
        else
        {
            bannerAccountName.text = "Anonymous";
            HostButton.interactable = false;
        }
    }

    void Awake()
    {
        bannerAccountImageDefault = bannerAccountImage;
    }
}
