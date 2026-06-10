using UnityEngine;

public class NpcInteraction : MonoBehaviour
{
    public DialogueYamlLoader dialogueLoader;
    public string promptText = "Press 'E' to Talk";
    public bool canInteract = true;

    private void Awake()
    {
        if (dialogueLoader == null)
        {
            dialogueLoader = GetComponent<DialogueYamlLoader>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
