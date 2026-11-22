using UnityEngine;
using UnityEngine.Rendering;

public class Toggles
{
    public static void NoVolume()
    {
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            v.enabled = false;
            Debug.Log("Disabled volume " + v.name);
        }
    }

    public static void Volume()
    {
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            v.enabled = true;
            Debug.Log("Enabled volume " + v.name);
        }
    }

    public static void Yaw(float yaw, float pitch, float distance)
    {
        var cf = Object.FindAnyObjectByType<CameraFollow>();
        if (cf == null) { Debug.Log("no camera follow"); return; }
        cf.yaw = yaw;
        cf.pitch = pitch;
        cf.distance = distance;
        Debug.Log("yaw=" + yaw + " pitch=" + pitch + " dist=" + distance);
    }
}
