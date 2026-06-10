using TMPro;
using UnityEngine;

public class InteractionPromptController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI interactionText;

    private void Awake()
    {
        if (interactionText == null)
        {
            interactionText = GetComponent<TextMeshProUGUI>();
        }    

        gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        interactionText.text = text;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
