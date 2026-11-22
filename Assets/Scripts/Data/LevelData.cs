using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public string levelName = "Level 1";
    public string sceneName = "Level1";
    public int rows = 5;
    public int columns = 4;
    public float tileSize = 1.2f;
    public float tileGap = 0.25f;
    public float timeLimit = 120f;
    public List<Vector2Int> safeTiles = new List<Vector2Int>();
    public List<Vector2Int> sequence = new List<Vector2Int>();
    public string objective = "";
    public string instructions = "";
    public bool hideMapAfterCountdown = false;
    public float mapVisibleDuration = 6.0f;
    public bool isUpsideDownPattern = false;
    public List<Vector2Int> displayedPattern = new List<Vector2Int>();
}
