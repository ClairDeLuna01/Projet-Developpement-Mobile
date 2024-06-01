using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using UnityEngine.Networking;
using System;

public class LobbyPlayerScript : MonoBehaviour
{
    public Toggle readyToggle;
    public TMP_Text usernameText;
    public Image userImage;

    public void SetUser(User user, bool ready = false)
    {
        usernameText.text = user.username;
        userImage.sprite = user.profilePictureSprite;
        readyToggle.isOn = ready;
    }
}
