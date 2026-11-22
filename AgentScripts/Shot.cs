using UnityEngine;

public class Shot
{
    public static void Run(string pitchArg, string distArg, string fovArg, string yawArg, string heightArg, string aheadArg)
    {
        var cf = Object.FindAnyObjectByType<CameraFollow>();
        var cam = Camera.main;
        if (cf == null || cam == null) { Debug.Log("missing camera"); return; }

        if (float.TryParse(pitchArg, out var pitch)) cf.pitch = pitch;
        if (float.TryParse(distArg, out var dist)) cf.distance = dist;
        if (float.TryParse(fovArg, out var fov)) cam.fieldOfView = fov;
        if (float.TryParse(yawArg, out var yaw)) cf.yaw = yaw;
        if (float.TryParse(heightArg, out var th)) cf.targetHeight = th;
        if (float.TryParse(aheadArg, out var ahead)) cf.lookAhead = ahead;

        cf.followSmooth = 60f;
        cf.rotationSmooth = 60f;
        Debug.Log("pitch=" + cf.pitch + " dist=" + cf.distance + " fov=" + cam.fieldOfView
            + " yaw=" + cf.yaw + " th=" + cf.targetHeight);
    }
}
