using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class resizeBasedOnChild : MonoBehaviour
{
    public TMP_Text childRectTransform;
    RectTransform rectTransform;
    float padding = 10;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, childRectTransform.preferredHeight + padding);
    }
}
