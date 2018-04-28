
using System.IO;
using UnityEngine;

public interface IGameStateManager
{
    GameState GameState { get; set; }
    void Load();
    void Save();
}

public class LocalGameStateManager : IGameStateManager
{
    public GameState GameState { get; set; }

    public virtual void Load()
    {
        if (!File.Exists(GAME_STATE_PATH))
        {
            return;
        }
        GameState = JsonUtility.FromJson<GameState>(File.ReadAllText(GAME_STATE_PATH));
    }

    public void Save()
    {
        Debug.Log("Saving game state to " + GAME_STATE_PATH);
        File.WriteAllText(GAME_STATE_PATH, JsonUtility.ToJson(GameState));
    }

    private readonly static string GAME_STATE_PATH = Path.Combine(Application.persistentDataPath, "gameState.json");
}

public class DebugGameStateManager : LocalGameStateManager
{
	public override void Load()
	{
        base.Load();
        File.WriteAllText(BACKUP_GAMESTATE_PATH, JsonUtility.ToJson(GameState));
	}

    public void SaveBackupAs(string name)
    {
        File.Copy(
            Path.Combine(Application.persistentDataPath, "gameStateBackup.json"),
            Path.Combine(Application.persistentDataPath, name + ".json"), true);
    }

    // public void RestoreBackupState()
    // {
    //     GameState = JsonUtility.FromJson<GameState>(File.ReadAllText(BACKUP_GAMESTATE_PATH));
    // }

    public void RestoreBackupState(string name)
    {
        var path = Path.Combine(Application.persistentDataPath, name + ".json");
        Debug.Log("Restoring state from " + path);
        GameState = JsonUtility.FromJson<GameState>(File.ReadAllText(path));
    }

    private static readonly string BACKUP_GAMESTATE_PATH
                            = Path.Combine(Application.persistentDataPath, "gameStateBackup.json");

}