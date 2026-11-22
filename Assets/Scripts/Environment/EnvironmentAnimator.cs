using UnityEngine;

/// <summary>Very light ambient motion: drifting cloud ring and bobbing scenery.</summary>
public class EnvironmentAnimator : MonoBehaviour
{
    public Transform cloudRoot;
    public float cloudRotationSpeed = 1.6f;

    void Update()
    {
        if (cloudRoot != null)
            cloudRoot.Rotate(0f, cloudRotationSpeed * Time.deltaTime, 0f, Space.World);
    }
}
