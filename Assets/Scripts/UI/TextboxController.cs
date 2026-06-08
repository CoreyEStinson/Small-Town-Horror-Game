using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextboxController : MonoBehaviour
{
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;
    public Image portrait;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenTextbox()
    {
        gameObject.SetActive(true);
    }

    public void CloseTextbox()
    {
        gameObject.SetActive(false);
    }

    public void SetSpeakerText(string text)
    {
        speakerText.text = text;
    }

    public void SetBodyText(string text)
    {
        bodyText.text = text;
    }

    public void SetPortraitImage(Sprite sprite)
    {
        portrait.sprite = sprite;
        portrait.SetNativeSize();
    }
}
