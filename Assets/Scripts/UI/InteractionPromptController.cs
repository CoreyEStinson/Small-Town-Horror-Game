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

        interactionText.gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        interactionText.text = text;
        interactionText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        interactionText.gameObject.SetActive(false);
    }
}
