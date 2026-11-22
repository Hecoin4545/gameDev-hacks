using UnityEngine;

public class TuneCamera
{
    public static void Run()
    {
        var cf = Object.FindAnyObjectByType<CameraFollow>();
        if (cf == null) { Debug.Log("No CameraFollow"); return; }
        cf.pitch = 27f;
        cf.distance = 12.5f;
        cf.followSmooth = 30f;
        Debug.Log("Camera tuned");
    }
}
