using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Defective.JSON;

[System.Serializable]
public class GameParameters
{
    int roundNumber;

    public static GameParameters CreateFromJSON(string jsonString)
    {
        JSONObject obj = new(jsonString);
        GameParameters parameters = new()
        {
            roundNumber = obj["roundNumber"].intValue
        };
        return parameters;
    }
}

[System.Serializable]
public class Game
{
    public List<int> players;
    public GameParameters parameters;
    public List<string> sentences;
    public List<bool> donePlayers;
    public int roundNumber;
    public bool done;

    public static Game CreateFromJSON(string jsonString)
    {
        if (jsonString == "null")
        {
            return null;
        }
        JSONObject obj = new(jsonString);
        Game game = new()
        {
            players = new List<int>()
        };
        foreach (JSONObject player in obj["players"].list)
        {
            game.players.Add(player.intValue);
        }
        game.parameters = GameParameters.CreateFromJSON(obj["parameters"].ToString());
        game.sentences = new List<string>();
        foreach (JSONObject sentence in obj["sentences"].list)
        {
            game.sentences.Add(sentence.stringValue);
        }
        game.donePlayers = new List<bool>();
        foreach (JSONObject donePlayer in obj["donePlayers"].list)
        {
            game.donePlayers.Add(donePlayer.boolValue);
        }
        game.roundNumber = obj["roundNumber"].intValue;
        game.done = obj["done"].boolValue;
        return game;
    }
}

[System.Serializable]
public class RoomInfoJson
{
    public int id;
    public string name;
    public string owner;
    public List<int> players;
    public Game game;
    public List<bool> readyPlayers;

    public static RoomInfoJson CreateFromJSON(string jsonString)
    {
        if (jsonString == "null")
        {
            return null;
        }
        JSONObject obj = new(jsonString);
        RoomInfoJson room = new()
        {
            id = obj["id"].intValue,
            name = obj["name"].stringValue,
            owner = obj["owner"].stringValue,
            players = new List<int>()
        };
        foreach (JSONObject player in obj["players"].list)
        {
            room.players.Add(player.intValue);
        }
        room.game = Game.CreateFromJSON(obj["game"].ToString());
        room.readyPlayers = new List<bool>();
        foreach (JSONObject readyPlayer in obj["readyPlayers"].list)
        {
            room.readyPlayers.Add(readyPlayer.boolValue);
        }
        return room;
    }
}

[System.Serializable]
public class RoomInfoJsonReq
{
    public bool success;
    public RoomInfoJson room;

    public static RoomInfoJsonReq CreateFromJSON(string jsonString)
    {
        JSONObject obj = new(jsonString);
        RoomInfoJsonReq roomReq = new()
        {
            success = obj["success"].boolValue,
            room = RoomInfoJson.CreateFromJSON(obj["room"].ToString())
        };
        return roomReq;
    }
}

[System.Serializable]
public class User
{
    public int id;
    public string username;
    public string email;
    public string passwordHash;

    public static User CreateFromJSON(string jsonString)
    {
        if (jsonString == "null")
        {
            return null;
        }
        JSONObject obj = new(jsonString);
        User user = new()
        {
            id = obj["id"].intValue,
            username = obj["username"].stringValue,
            email = obj["email"].stringValue,
            passwordHash = obj["passwordHash"].stringValue
        };
        return user;
    }
}

[System.Serializable]
public class RoomListJsonReq
{
    public bool success;
    public List<RoomInfoJson> rooms;

    public static RoomListJsonReq CreateFromJSON(string jsonString)
    {
        JSONObject obj = new(jsonString);
        RoomListJsonReq roomListReq = new()
        {
            success = obj["success"].boolValue,
            rooms = new List<RoomInfoJson>()
        };

        foreach (JSONObject room in obj["rooms"].list)
        {
            roomListReq.rooms.Add(RoomInfoJson.CreateFromJSON(room.ToString()));
        }
        return roomListReq;
    }
}


public class GameManager : MonoBehaviour
{
    private const string roomListURL = "https://clairdeluna.pythonanywhere.com/rooms";

    public User user;
    public RoomInfoJson roomInfo;

    public GameObject canvas;
    public GameObject roomListItemPrefab;

    public List<RoomInfoJson> roomsDisplay;

    enum GameState
    {
        BROWSING,
        IN_ROOM,
        IN_GAME
    }
    GameState state = GameState.BROWSING;

    public void CreateRoom()
    {
        StartCoroutine(CreateRoomCoroutine());
    }

    public void LeaveRoom()
    {
        StartCoroutine(LeaveRoomCoroutine());
    }

    public void JoinRoom(int roomID, string password = "")
    {
        StartCoroutine(JoinRoomCoroutine(roomID, password));
    }

    IEnumerator CreateRoomCoroutine(string roomName = "Room1")
    {
        string url = $"{roomListURL}/createRoom/{user.id}/roomName";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            RoomInfoJsonReq resp = RoomInfoJsonReq.CreateFromJSON(webRequest.downloadHandler.text);
            if (resp.success)
            {
                Debug.Log("Room: " + resp.room.name);
                Debug.Log("Owner: " + resp.room.owner);
                Debug.Log("Players: " + resp.room.players);
                roomInfo = resp.room;
            }
            else
            {
                Debug.Log("Failed to create room");
            }
        }
    }

    IEnumerator LeaveRoomCoroutine()
    {
        if (roomInfo.id == 0)
        {
            Debug.Log("No room to leave");
            yield break;
        }
        string url = $"{roomListURL}/leaveRoom/{user.id}/{roomInfo.id}";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            RoomInfoJsonReq resp = RoomInfoJsonReq.CreateFromJSON(webRequest.downloadHandler.text);
            if (resp.success)
            {
                Debug.Log("Room: " + resp.room.name);
                Debug.Log("Owner: " + resp.room.owner);
                Debug.Log("Players: " + resp.room.players);
                roomInfo = null;
            }
            else
            {
                Debug.Log("Failed to leave room"); // 😰
            }
        }
    }

    IEnumerator JoinRoomCoroutine(int roomID, string password = "")
    {
        RoomInfoJson roomInfoQuery = null;
        yield return GetRoomInfoCoroutine(roomID, (room) => roomInfoQuery = room);

        if (roomInfoQuery == null)
        {
            Debug.Log("Room not found");
            yield break;
        }

        string url;
        if (password == "")
        {
            url = $"{roomListURL}/joinRoom/{user.id}/{roomInfoQuery.id}";
        }
        else
        {
            url = $"{roomListURL}/joinRoom/{user.id}/{roomInfoQuery.id}/{password}";
        }

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            RoomInfoJsonReq resp = RoomInfoJsonReq.CreateFromJSON(webRequest.downloadHandler.text);
            if (resp.success)
            {
                Debug.Log("Room: " + resp.room.name);
                Debug.Log("Owner: " + resp.room.owner);
                Debug.Log("Players: " + resp.room.players);
                roomInfo = resp.room;
            }
            else
            {
                Debug.Log("Failed to join room"); // 😰
            }
        }
    }

    IEnumerator GetRoomInfoCoroutine(int roomID, Action<RoomInfoJson> callback)
    {
        string url = $"{roomListURL}/getRoom/{roomID}";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            RoomInfoJsonReq resp = RoomInfoJsonReq.CreateFromJSON(webRequest.downloadHandler.text);
            if (resp.success)
            {
                Debug.Log("Room: " + resp.room.name);
                Debug.Log("Owner: " + resp.room.owner);
                Debug.Log("Players: " + resp.room.players);
                roomInfo = resp.room;
                callback(roomInfo);
            }
            else
            {
                Debug.Log("Failed to get room info"); // 😰
            }
        }
    }

    IEnumerator GetRoomListCoroutine(Action<List<RoomInfoJson>> callback)
    {
        string url = $"{roomListURL}/getRooms";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            RoomListJsonReq resp = RoomListJsonReq.CreateFromJSON(webRequest.downloadHandler.text);
            if (resp.success)
            {
                Debug.Log("Rooms: " + resp.rooms);
                Debug.Log("Rooms.size: " + resp.rooms.Count);
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
        List<RoomInfoJson> rooms = null;
        yield return GetRoomListCoroutine((roomList) => rooms = roomList);

        Debug.Log("Rooms: " + rooms);
        Debug.Log("Rooms.size: " + rooms.Count);
        roomsDisplay = rooms;

        if (rooms == null)
        {
            Debug.Log("No rooms found");
            yield break;
        }

        foreach (Transform child in canvas.transform)
        {
            if (child.CompareTag("RoomInfoPrefab"))
                Destroy(child.gameObject);
        }

        int i = 0;
        foreach (RoomInfoJson room in rooms)
        {
            Debug.Log("Room: " + room.name);
            int offset = -i * 130;
            GameObject roomListItem = Instantiate(roomListItemPrefab, canvas.transform);
            roomListItem.GetComponent<RoomInfoDisplay>().UpdateRoomInfo(room);
            roomListItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, offset);

            i++;
        }
    }

    IEnumerator UpdateDisplayLoop()
    {
        while (true)
        {
            if (state == GameState.BROWSING)
            {
                StartCoroutine(UpdateRoomsDisplayCoroutine());
                yield return new WaitForSeconds(5);
            }
        }
    }

    public bool updateDisplay = true;


    // Start is called before the first frame update
    void Start()
    {
        // temporary
        user = new User
        {
            id = 1,
            username = "user1",
            email = "test@mail.com",
            passwordHash = "password"
        };

        if (updateDisplay)
            StartCoroutine(UpdateDisplayLoop());
    }

    // Update is called once per frame
    void Update()
    {

    }
}
