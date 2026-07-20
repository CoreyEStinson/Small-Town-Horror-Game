using System.Collections.Generic;
public sealed class SaveGameData
{
    public int saveVersion = 1;

    public List<string> activeFlags = new List<string>();
    
    public string checkpointSceneName = string.Empty;
    public string checkpointSpawnId = string.Empty;
    public JournalSaveData journal = new JournalSaveData();
}
