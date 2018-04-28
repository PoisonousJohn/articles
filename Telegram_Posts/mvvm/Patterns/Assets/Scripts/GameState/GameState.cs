[System.Serializable]
public class GameState
{
    [System.Serializable]
    public class GameSettings
    {
        public bool soundEnabled { get; set; }
        public bool musicEnabled { get; set; }
        public float musicLevel { get; set; }
        public float soundLevel { get; set; }
    }

    public GameSettings settings { get; set; }
}