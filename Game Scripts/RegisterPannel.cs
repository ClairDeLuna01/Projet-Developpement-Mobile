using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using System.Security.Cryptography;
using UnityEngine.Networking;
using SFB;

public class RegisterPannel : MonoBehaviour
{
    public TMP_InputField username;
    public TMP_InputField email;
    public TMP_InputField password;
    public Button SelectProfilePicButton;
    public Image ProfilePic;
    public Texture2D DefaultProfilePic;
    private byte[] profilePicBytes = null;
    public Toggle PremiumToggle;
    public Button RegisterButton;
    public Button LoginButton;

    public void OnPressSelectProfilePic()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Select Profile Picture", "", "png,jpg,jpeg");
        OnPressSelectProfilePic(path);
#else
        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            ExtensionFilter filter = new ExtensionFilter("Image Files", "jpg", "png");
            ExtensionFilter[] filters = { filter };
            string path = StandaloneFileBrowser.OpenFilePanel("Select Profile Picture", "", filters, false)[0];
            OnPressSelectProfilePic(path);
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            string[] extensions = { "image/*" };
            NativeFilePicker.PickFile((string pickedPath) =>
            {
                if (pickedPath != null)
                    OnPressSelectProfilePic(pickedPath);
            }, extensions);
        }
        else
        {
            Debug.LogError("Unsupported platform");
        }
#endif
    }

    public void OnPressSelectProfilePic(string path)
    {
        if (path.Length != 0)
        {
            profilePicBytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(profilePicBytes);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            ProfilePic.sprite = sprite;
        }
        else
        {
            profilePicBytes = null;
            ProfilePic.sprite = Sprite.Create(DefaultProfilePic, new Rect(0, 0, DefaultProfilePic.width, DefaultProfilePic.height), new Vector2(0.5f, 0.5f));
        }
    }

    IEnumerator RegisterUser()
    {
        RegisterButton.interactable = false;
        // hash the password
        byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password.text);
        byte[] hashedPasswordBytes = new SHA256Managed().ComputeHash(passwordBytes);
        // convert the hashed password to a hex string
        string hashedPassword = System.BitConverter.ToString(hashedPasswordBytes).Replace("-", "").ToLower();
        if (profilePicBytes == null)
        {
            string url = $"{GameManager.serverURL}/user/register/{username.text}/{hashedPassword}/{email.text}/{PremiumToggle.isOn}";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(webRequest.error);
                RegisterButton.interactable = true;
            }
            else
            {
                JSONLoginResponse response = JSONLoginResponse.CreateFromJSON(webRequest.downloadHandler.text);
                if (response.success)
                {
                    Debug.Log("User registered successfully");
                    RegisterButton.interactable = true;
                    Debug.Log("change gamestate here to login");
                }
                else
                {
                    Debug.LogError("User registration failed");
                    RegisterButton.interactable = true;
                }
            }
        }
        else
        {
            string url = $"{GameManager.serverURL}/user/register/{username.text}/{hashedPassword}/{email.text}/{PremiumToggle.isOn}";
            WWWForm form = new WWWForm();
            form.AddBinaryData("image", profilePicBytes);
            using UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(webRequest.error);
                RegisterButton.interactable = true;
            }
            else
            {
                JSONLoginResponse response = JSONLoginResponse.CreateFromJSON(webRequest.downloadHandler.text);
                if (response.success)
                {
                    Debug.Log("User registered successfully");
                    RegisterButton.interactable = true;
                    GameManager.ChangeGameState(GameManager.GameState.LOGIN);
                }
                else
                {
                    Debug.LogError("User registration failed");
                    RegisterButton.interactable = true;
                }
            }
        }
    }

    public void OnPressRegister()
    {
        StartCoroutine(RegisterUser());
    }

    public void OnPressLogin()
    {
        GameManager.ChangeGameState(GameManager.GameState.LOGIN);
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
