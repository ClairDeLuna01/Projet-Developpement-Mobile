using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using System.Security.Cryptography;
using UnityEngine.Networking;
using UnityEngine;

public class LoginPanel : MonoBehaviour
{
    public TMP_InputField username;
    public TMP_InputField password;
    public Button LoginButton;
    public Button RegisterButton;

    IEnumerator LoginUser()
    {
        LoginButton.interactable = false;
        // hash the password
        byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password.text);
        byte[] hashedPasswordBytes = new SHA256Managed().ComputeHash(passwordBytes);
        // convert the hashed password to a hex string
        string hashedPassword = System.BitConverter.ToString(hashedPasswordBytes).Replace("-", "").ToLower();
        string url = $"{GameManager.serverURL}/user/login/{username.text}/{hashedPassword}";
        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(webRequest.error);
            LoginButton.interactable = true;
        }
        else
        {
            string response = webRequest.downloadHandler.text;
            Debug.Log(response);
            JSONUserInfo userInfo = JSONUserInfo.CreateFromJSON(webRequest.downloadHandler.text);
            LoginButton.interactable = true;
            if (userInfo.success)
            {
                GameManager.user = User.FromJsonUserInfo(userInfo);
                Debug.Log("Login successful");
                GameManager.ChangeGameState(GameManager.GameState.BROWSING);
            }
            else
            {
                Debug.LogError("Login failed");
            }
        }
    }

    public void OnPressLogin()
    {
        StartCoroutine(LoginUser());
    }

    public void OnPressRegister()
    {
        GameManager.ChangeGameState(GameManager.GameState.REGISTER);
    }

    public void Update()
    {
        // get back button
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.ChangeGameState(GameManager.GameState.BROWSING);
        }
    }
}
