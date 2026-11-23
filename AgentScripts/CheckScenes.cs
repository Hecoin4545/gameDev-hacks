using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class CheckScenes
{
    public static void Run()
    {
        // ── Check Level 2 ─────────────────────────────────────────────────────
        var scene2 = EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity", OpenSceneMode.Single);
        var lm2 = Object.FindAnyObjectByType<LevelManager>();
        if (lm2 == null) Debug.LogError("Level 2: LevelManager missing!");
        else
        {
            Debug.Log(string.Format("Level 2 OK: data={0}, hideMap={1}, dur={2}s, narrator={3}",
                lm2.data.levelName, lm2.data.hideMapAfterCountdown, lm2.data.mapVisibleDuration,
                lm2.levelNarratorTexts != null ? lm2.levelNarratorTexts.Length : 0));
        }

        var player2 = GameObject.FindWithTag("Player");
        var start2  = GameObject.Find("StartIsland");
        var end2    = GameObject.Find("EndIsland");
        var grid2   = GameObject.Find("Grid");
        Debug.Log(string.Format("Level 2 objects: player={0}, start={1}, end={2}, grid={3}",
            player2 != null, start2 != null, end2 != null, grid2 != null));

        // ── Check Level 3 ─────────────────────────────────────────────────────
        var scene3 = EditorSceneManager.OpenScene("Assets/Scenes/Level3.unity", OpenSceneMode.Single);
        var lm3 = Object.FindAnyObjectByType<LevelManager>();
        if (lm3 == null) Debug.LogError("Level 3: LevelManager missing!");
        else
        {
            Debug.Log(string.Format("Level 3 OK: data={0}, hideMap={1}, dur={2}s, narrator={3}",
                lm3.data.levelName, lm3.data.hideMapAfterCountdown, lm3.data.mapVisibleDuration,
                lm3.levelNarratorTexts != null ? lm3.levelNarratorTexts.Length : 0));
        }

        var player3   = GameObject.FindWithTag("Player");
        var start3    = GameObject.Find("StartIsland");
        var end3      = GameObject.Find("EndIsland");
        var grid3     = GameObject.Find("Grid");
        var snow3     = GameObject.Find("Snowfall");
        var env3      = GameObject.Find("Environment");
        var ps3       = snow3 != null ? snow3.GetComponent<ParticleSystem>() : null;

        Debug.Log(string.Format("Level 3 objects: player={0}, start={1}, end={2}, grid={3}, snowfall={4} (ps={5}), env={6}",
            player3 != null, start3 != null, end3 != null, grid3 != null, snow3 != null, ps3 != null, env3 != null));

        if (env3 != null)
        {
            int treeCount = 0;
            int spireCount = 0;
            int glacierCount = 0;
            int peakCount = 0;
            foreach (Transform t in env3.transform)
            {
                if (t.name.Contains("Pine")) treeCount++;
                if (t.name.Contains("Spire")) spireCount++;
                if (t.name.Contains("Glacier")) glacierCount++;
                if (t.name.Contains("Peak")) peakCount++;
            }
            Debug.Log(string.Format("Level 3 Environment details: PineTrees={0}, Spires={1}, Glaciers={2}, AlpinePeaks={3}",
                treeCount, spireCount, glacierCount, peakCount));
        }
    }
}
