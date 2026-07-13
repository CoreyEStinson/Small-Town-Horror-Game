using UnityEngine;

[CreateAssetMenu(
    fileName = "DialogueBarkProfile",
    menuName = "Dialogue/Bark Profile")]
public class DialogueBarkProfile : ScriptableObject
{
    [SerializeField] private string npcId;

    [Header("Audio")]
    [SerializeField] private AudioClip[] clips;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.45f;
    [Min(1)] [SerializeField] private int charactersPerBlip = 2;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    public string NpcId => npcId;
    public float Volume => volume;
    public int CharactersPerBlip => Mathf.Max(1, charactersPerBlip);

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        for (int attempt = 0; attempt < clips.Length; attempt++)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    public float GetRandomPitch()
    {
        float min = Mathf.Min(pitchRange.x, pitchRange.y);
        float max = Mathf.Max(pitchRange.x, pitchRange.y);
        return Random.Range(min, max);
    }
}
