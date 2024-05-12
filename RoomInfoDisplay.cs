using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomInfoDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text RoomNameInput;
    [SerializeField] private TMP_Text PlayerNumberInput;

    public void UpdateRoomInfo(RoomInfoJson roomInfo)
    {
        RoomNameInput.text = roomInfo.name;
        PlayerNumberInput.text = roomInfo.players.Count.ToString();
    }

    public void OnPointerClick()
    {
        Debug.Log("RoomInfoDisplay clicked");
    }
}
