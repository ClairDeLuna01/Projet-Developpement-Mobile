using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDoneScript : MonoBehaviour
{
    public User user;
    public Image readyIndicator;
    public Image userImage;

    public void SetUser(User user)
    {
        this.user = user;
        userImage.sprite = user.profilePictureSprite;
        readyIndicator.color = Color.red;
    }

    public void updateIndicator(bool isReady)
    {
        readyIndicator.color = isReady ? Color.green : Color.red;
    }
}
