using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Defective.JSON;

[System.Serializable]
public class JSONGame
{
    public int id;
    public List<int> players;
    public int roundMax;
    public List<string> sentences;
    public List<bool> donePlayers;
    public int roundNumber;
    public bool done;
    public int roomID;

    public static JSONGame CreateFromJSON(string jsonString)
    {
        JSONObject obj = new(jsonString);
        JSONGame game = new()
        {
            id = obj["id"].intValue,
            players = new List<int>(),
            sentences = new List<string>(),
            donePlayers = new List<bool>()
        };
        foreach (JSONObject player in obj["players"].list)
        {
            game.players.Add(player.intValue);
        }
        game.roundMax = obj["roundMax"].intValue;
        foreach (JSONObject sentence in obj["sentences"].list)
        {
            game.sentences.Add(sentence.stringValue);
        }
        foreach (JSONObject donePlayer in obj["donePlayers"].list)
        {
            game.donePlayers.Add(donePlayer.boolValue);
        }
        game.roundNumber = obj["roundNumber"].intValue;
        game.done = obj["done"].intValue != 0;
        game.roomID = obj["roomID"].intValue;
        return game;
    }
}

[System.Serializable]
public class JSONRoomInfo
{
    public int id;
    public string name;
    public int owner;
    public List<int> players;
    public int gameID;
    public List<bool> readyPlayers;
    public int roundMax;
    public bool usesPassword;
    public JSONGame game;

    public static JSONRoomInfo CreateFromJSON(string jsonString)
    {
        if (jsonString == "null")
            return null;
        JSONObject obj = new(jsonString);
        JSONRoomInfo roomInfo = new()
        {
            id = obj["id"].intValue,
            name = obj["name"].stringValue,
            owner = obj["owner"].intValue,
            players = new List<int>(),
            readyPlayers = new List<bool>(),
            usesPassword = obj["usesPassword"].boolValue
        };
        foreach (JSONObject player in obj["players"].list)
        {
            roomInfo.players.Add(player.intValue);
        }
        roomInfo.gameID = obj["gameID"].intValue;
        foreach (JSONObject readyPlayer in obj["readyPlayers"].list)
        {
            roomInfo.readyPlayers.Add(readyPlayer.boolValue);
        }
        roomInfo.roundMax = obj["roundMax"].intValue;
        if (obj["game"].ToString() != "null")
            roomInfo.game = JSONGame.CreateFromJSON(obj["game"].ToString());
        else
            roomInfo.game = null;
        return roomInfo;
    }
}

[System.Serializable]
public class JSONRoomResponse
{
    public bool success;
    public JSONRoomInfo room;

    public static JSONRoomResponse CreateFromJSON(string jsonString)
    {
        JSONObject obj = new(jsonString);
        JSONRoomResponse response = new()
        {
            success = obj["success"].boolValue
        };
        if (obj["room"].ToString() == "null")
            response.room = null;
        else
            response.room = JSONRoomInfo.CreateFromJSON(obj["room"].ToString());
        return response;
    }
}

[System.Serializable]
public class JSONRoomListResponse
{
    public bool success;
    public List<JSONRoomInfo> rooms;

    public static JSONRoomListResponse CreateFromJSON(string jsonString)
    {
        JSONObject obj = new(jsonString);
        JSONRoomListResponse response = new()
        {
            success = obj["success"].boolValue,
            rooms = new List<JSONRoomInfo>()
        };
        if (obj["rooms"].ToString() == "[]")
            return response;
        foreach (JSONObject room in obj["rooms"].list)
        {
            response.rooms.Add(JSONRoomInfo.CreateFromJSON(room.ToString()));
        }
        return response;
    }
}

[System.Serializable]
public class JSONLoginResponse
{
    public bool success;
    public int userId;

    public static JSONLoginResponse CreateFromJSON(string jsonString)
    {
        JSONObject obj = new(jsonString);
        JSONLoginResponse response = new()
        {
            success = obj["success"].boolValue,
            userId = obj["id"].intValue
        };
        return response;
    }
}

[System.Serializable]
public class JSONUserInfo
{
    public bool success;
    public int id;
    public string username;
    public string email;
    public string passwordHash;
    public string profilePictureB64;
    public bool premium;

    public static JSONUserInfo CreateFromJSON(string jsonString)
    {
        JSONObject obj = new(jsonString);
        JSONUserInfo userInfo = new()
        {
            success = obj["success"].boolValue,
            id = obj["id"].intValue,
            username = obj["username"].stringValue,
            email = obj["email"].stringValue,
            passwordHash = obj["passwordHash"].stringValue,
            premium = obj["premium"].intValue == 1
        };
        string pic = obj["profilePicB64"].stringValue;
        if (pic != "null")
        {
            userInfo.profilePictureB64 = pic;
        }
        else
        {
            userInfo.profilePictureB64 = null;
        }
        return userInfo;
    }
}