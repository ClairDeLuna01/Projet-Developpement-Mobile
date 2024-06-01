using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PasswordPopupScript : MonoBehaviour
{
    public TMP_InputField passwordInput;
    public Button submitButton;

    public string passwordInputText = "";
    public bool submitted = false;

    public void OnSubmit()
    {
        passwordInputText = passwordInput.text;
        submitted = true;
    }
}
