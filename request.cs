using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;



public class request : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetRequest("https://clairdeluna.pythonanywhere.com/db/getUser/1");
    }

    public void GetRequest(string url)
    {
        StartCoroutine(GetRequestCoroutine(url));
    }

    IEnumerator GetRequestCoroutine(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                Debug.Log("Received: " + webRequest.downloadHandler.text);
                User user = User.CreateFromJSON(webRequest.downloadHandler.text);
                Debug.Log("User: " + user.username);
                Debug.Log("Email: " + user.email);
                Debug.Log("Password: " + user.passwordHash);
                Debug.Log("ID: " + user.id);

            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
