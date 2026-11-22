using UnityEngine;

/// <summary>
/// Procedural low-poly character animation for the primitive-built adventurer.
/// Swings limbs while walking, tucks them while airborne, and adds a landing squash.
/// </summary>
public class PlayerVisual : MonoBehaviour
{
    public Transform model;
    public Transform torso;
    public Transform head;
    public Transform armL;
    public Transform armR;
    public Transform legL;
    public Transform legR;

    public float walkCycleSpeed = 7.5f;
    public float turnSpeed = 14f;

    private PlayerController controller;
    private CharacterController cc;
    private Quaternion armLBase, armRBase, legLBase, legRBase;
    private Vector3 modelBasePos;
    private float phase;
    private float squashTime;
    private float squashStrength;
    private float jumpStretch;
    private bool wasGrounded = true;
    private bool dead;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        cc = GetComponent<CharacterController>();
        if (model != null) modelBasePos = model.localPosition;
        if (armL != null) armLBase = armL.localRotation;
        if (armR != null) armRBase = armR.localRotation;
        if (legL != null) legLBase = legL.localRotation;
        if (legR != null) legRBase = legR.localRotation;
    }

    void Update()
    {
        if (dead)
        {
            if (model != null)
            {
                model.Rotate(0f, 420f * Time.deltaTime, 0f, Space.World);
                model.localScale = Vector3.Lerp(model.localScale, Vector3.one * 0.15f, 6f * Time.deltaTime);
            }
            return;
        }

        float dt = Time.deltaTime;
        if (cc == null) return;

        Vector3 planar = cc.velocity;
        planar.y = 0f;
        float speed = planar.magnitude;
        float moveAmount = Mathf.Clamp01(speed / 6f);
        bool grounded = cc.isGrounded;

        if (grounded && !wasGrounded)
        {
            squashTime = 0.18f;
            squashStrength = Mathf.Clamp01(1f - speed * 0.05f);
        }
        if (!grounded && wasGrounded)
        {
            jumpStretch = 0.2f;
        }
        wasGrounded = grounded;

        // Face the direction of travel.
        if (model != null && controller != null)
        {
            Vector3 dir = controller.MoveDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0025f)
            {
                Quaternion want = Quaternion.LookRotation(dir.normalized, Vector3.up);
                model.rotation = Quaternion.Slerp(model.rotation, want, 1f - Mathf.Exp(-turnSpeed * dt));
            }
        }

        if (speed > 0.12f && grounded)
            phase += dt * (5f + walkCycleSpeed * moveAmount);
        else if (grounded)
            phase = Mathf.Lerp(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, 10f * dt);

        float swing = Mathf.Sin(phase) * 42f * moveAmount;
        float targetArmL, targetArmR, targetLegL, targetLegR;

        if (grounded)
        {
            targetArmL = swing;
            targetArmR = -swing;
            targetLegL = -swing * 0.9f;
            targetLegR = swing * 0.9f;
        }
        else
        {
            targetArmL = -75f;
            targetArmR = -75f;
            targetLegL = 28f;
            targetLegR = -22f;
        }

        float limbBlend = 1f - Mathf.Exp(-14f * dt);
        if (armL != null) armL.localRotation = Quaternion.Slerp(armL.localRotation, armLBase * Quaternion.Euler(targetArmL, 0f, 0f), limbBlend);
        if (armR != null) armR.localRotation = Quaternion.Slerp(armR.localRotation, armRBase * Quaternion.Euler(targetArmR, 0f, 0f), limbBlend);
        if (legL != null) legL.localRotation = Quaternion.Slerp(legL.localRotation, legLBase * Quaternion.Euler(targetLegL, 0f, 0f), limbBlend);
        if (legR != null) legR.localRotation = Quaternion.Slerp(legR.localRotation, legRBase * Quaternion.Euler(targetLegR, 0f, 0f), limbBlend);

        if (model != null)
        {
            float bob = grounded ? Mathf.Abs(Mathf.Sin(phase)) * 0.055f * moveAmount : 0f;
            model.localPosition = Vector3.Lerp(model.localPosition, modelBasePos + Vector3.up * bob, 12f * dt);

            if (squashTime > 0f) squashTime -= dt;
            if (jumpStretch > 0f) jumpStretch -= dt;

            float squash = squashTime > 0f ? (squashTime / 0.18f) * 0.16f * squashStrength : 0f;
            float stretch = jumpStretch > 0f ? (jumpStretch / 0.2f) * 0.1f : 0f;
            Vector3 scale = new Vector3(1f + squash - stretch * 0.5f, 1f - squash + stretch, 1f + squash - stretch * 0.5f);
            model.localScale = Vector3.Lerp(model.localScale, scale, 16f * dt);
        }
    }

    public void PlayDeath()
    {
        dead = true;
        if (cc != null) cc.enabled = false;
        if (controller != null) controller.enabled = false;
    }
}
