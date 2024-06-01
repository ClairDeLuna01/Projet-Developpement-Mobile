using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using System.Security.Cryptography;
using UnityEngine.Networking;
using System;

public class GameEndScript : MonoBehaviour
{
    public GameObject submitObject;
    public Button submitButton;
    public GameObject SentenceDisplayPrefab;
    public GameObject SentenceDisplayParent;
    public ScrollableContent scrollableContent;
    private bool updateChildrenPositionsNextFrame = false;

    void PopulateSentences()
    {
        foreach (Transform child in SentenceDisplayParent.transform)
        {
            if (child.gameObject != submitObject)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (string sentence in GameManager.roomInfo.game.sentences)
        {
            GameObject sentenceDisplay = Instantiate(SentenceDisplayPrefab, SentenceDisplayParent.transform);
            sentenceDisplay.GetComponent<SentenceDisplayScript>().sentenceText.text = sentence;
        }

        submitButton.transform.SetAsLastSibling();
        updateChildrenPositionsNextFrame = true;
    }

    void OnEnable()
    {
        PopulateSentences();
    }

    public void OnSubmit()
    {
        LobbyScript.LeaveLobby(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnSubmit();
        }

        if (updateChildrenPositionsNextFrame)
        {
            scrollableContent.updateChildrenPositions = true;
            updateChildrenPositionsNextFrame = false;
        }
    }
}
