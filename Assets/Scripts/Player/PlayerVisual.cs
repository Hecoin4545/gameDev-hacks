using UnityEngine;

/// <summary>
/// Procedural low-poly character animation for the primitive-built adventurer.
/// Swings limbs while walking, tucks them while airborne, and adds a landing squash.
/// Press F to trigger a stylish, professional procedural dance routine with rhythmic
/// head bobs, torso groove, arm gestures, hip bounce, and smooth blending.
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
    private Quaternion torsoBase, headBase;
    private Vector3 modelBasePos;
    private float phase;
    private float squashTime;
    private float squashStrength;
    private float jumpStretch;
    private bool wasGrounded = true;
    private bool dead;

    // Professional Dance state
    private bool dancing;
    private float danceTimer;
    private float danceWeight;      // 0 to 1 smooth fade in/out to prevent any snapping
    private float dancePhase;       // primary beat phase
    private float dancePhase2;      // syncopated phase
    private int   danceBeat;        // quantized beat counter
    private float danceBeatTimer;   // time within current beat
    private const float DanceDuration = 4.2f;
    private const float DanceBPM      = 124f;   // 124 BPM upbeat pop rhythm
    private const float BeatInterval  = 60f / DanceBPM;

    // Fall state
    private bool falling;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        cc = GetComponent<CharacterController>();
        if (model != null) modelBasePos = model.localPosition;
        if (armL  != null) armLBase  = armL.localRotation;
        if (armR  != null) armRBase  = armR.localRotation;
        if (legL  != null) legLBase  = legL.localRotation;
        if (legR  != null) legRBase  = legR.localRotation;
        if (torso != null) torsoBase = torso.localRotation;
        if (head  != null) headBase  = head.localRotation;
    }

    public void TriggerDance()
    {
        if (dead || falling) return;
        dancing        = true;
        danceTimer     = DanceDuration;
        dancePhase     = 0f;
        dancePhase2    = 0f;
        danceBeat      = 0;
        danceBeatTimer = 0f;
        AudioManager.Instance?.PlayDance();
    }

    public void PlayFall()
    {
        falling = true;
        dancing = false;
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

        if (falling)
        {
            if (model != null)
            {
                model.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
                model.localScale = Vector3.Lerp(model.localScale, Vector3.zero, 3f * Time.deltaTime);
            }
            return;
        }

        float dt = Time.deltaTime;
        if (cc == null) return;

        Vector3 planar = cc.velocity;
        planar.y = 0f;
        float speed      = planar.magnitude;
        float moveAmount = Mathf.Clamp01(speed / 6f);
        bool  grounded   = cc.isGrounded;

        // Cancel dance immediately on significant movement or jumping
        if (speed > 0.4f || !grounded)
        {
            dancing = false;
        }

        if (grounded && !wasGrounded)
        {
            squashTime     = 0.18f;
            squashStrength = Mathf.Clamp01(1f - speed * 0.05f);
            dancing        = false;
        }
        if (!grounded && wasGrounded)
        {
            jumpStretch = 0.2f;
            dancing     = false;
        }
        wasGrounded = grounded;

        // Dance timer decrement
        if (dancing)
        {
            danceTimer -= dt;
            if (danceTimer <= 0f) dancing = false;
        }

        // Smooth fade-in and fade-out of dance weight (avoids jerky pops)
        danceWeight = Mathf.MoveTowards(danceWeight, dancing ? 1f : 0f, 7f * dt);

        // ── PROFESSIONAL DANCE CHOREOGRAPHY CALCULATIONS ──────────────────────
        float danceArmLAngle = 0f, danceArmRAngle = 0f;
        float danceArmLZ     = 20f, danceArmRZ    = -20f;
        float danceLegLAngle = 0f, danceLegRAngle = 0f;
        float danceTorsoLean = 0f, danceTorsoYaw  = 0f;
        float danceHeadNod   = 0f, danceHeadTilt  = 0f;
        float danceHipBounce = 0f;
        float danceBodyYaw   = 0f;

        if (danceWeight > 0.001f)
        {
            dancePhase  += dt * (DanceBPM / 60f) * Mathf.PI * 2f;
            dancePhase2 += dt * (DanceBPM / 60f) * Mathf.PI * 4f;

            danceBeatTimer += dt;
            if (danceBeatTimer >= BeatInterval)
            {
                danceBeatTimer -= BeatInterval;
                danceBeat++;
            }

            // Smooth ease in/out for beat transitions
            float t = Mathf.Clamp01(danceBeatTimer / BeatInterval);
            float st = Mathf.SmoothStep(0f, 1f, t);

            // Rhythmic whole-body musical bounce on the eighth-notes
            danceHipBounce = Mathf.Sin(dancePhase2) * 0.045f;
            danceHeadNod   = Mathf.Sin(dancePhase2) * 7.5f;

            // 4-measure dynamic routine (16 beats total, cycling smoothly)
            int step = danceBeat % 8;

            switch (step)
            {
                case 0:
                case 1:
                    // Groove Sway Left: torso tilts, right arm pumps rhythmically, left arm holds tempo
                    danceTorsoLean = Mathf.Lerp(-6f, -10f, st);
                    danceTorsoYaw  = Mathf.Sin(dancePhase) * 14f;
                    danceHeadTilt  = -danceTorsoLean * 0.6f;
                    danceArmLAngle = Mathf.Sin(dancePhase) * 35f - 15f;
                    danceArmRAngle = Mathf.Cos(dancePhase) * 65f + 30f;
                    danceArmLZ     = 35f;
                    danceArmRZ     = -45f;
                    danceLegLAngle = -8f;
                    danceLegRAngle = 12f;
                    danceBodyYaw   = -8f;
                    break;

                case 2:
                case 3:
                    // Groove Sway Right: weight transfers right, opposite arm pump
                    danceTorsoLean = Mathf.Lerp(6f, 10f, st);
                    danceTorsoYaw  = Mathf.Sin(dancePhase) * 14f;
                    danceHeadTilt  = -danceTorsoLean * 0.6f;
                    danceArmLAngle = Mathf.Cos(dancePhase) * 65f + 30f;
                    danceArmRAngle = Mathf.Sin(dancePhase) * 35f - 15f;
                    danceArmLZ     = 45f;
                    danceArmRZ     = -35f;
                    danceLegLAngle = 12f;
                    danceLegRAngle = -8f;
                    danceBodyYaw   = 8f;
                    break;

                case 4:
                case 5:
                    // Center Wave: arms sweep up with rhythmic wrist elevation, hip rolls
                    danceTorsoLean = Mathf.Sin(dancePhase2) * 4f;
                    danceTorsoYaw  = Mathf.Cos(dancePhase) * 18f;
                    danceArmLAngle = Mathf.Lerp(45f, 95f, (Mathf.Sin(dancePhase) + 1f) * 0.5f);
                    danceArmRAngle = Mathf.Lerp(45f, 95f, (Mathf.Cos(dancePhase) + 1f) * 0.5f);
                    danceArmLZ     = 55f;
                    danceArmRZ     = -55f;
                    danceLegLAngle = Mathf.Sin(dancePhase) * 12f;
                    danceLegRAngle = -Mathf.Sin(dancePhase) * 12f;
                    danceBodyYaw   = Mathf.Sin(dancePhase) * 10f;
                    break;

                default: // 6 & 7
                    // Stylish Pose & Drop: crisp accent pose, head snap, arms cross/flair
                    danceTorsoLean = -2f;
                    danceTorsoYaw  = Mathf.Sin(dancePhase2) * 8f;
                    danceArmLAngle = Mathf.LerpUnclamped(-35f, 75f, st);
                    danceArmRAngle = Mathf.LerpUnclamped(75f, -35f, st);
                    danceArmLZ     = 25f;
                    danceArmRZ     = -25f;
                    danceLegLAngle = -14f;
                    danceLegRAngle = 14f;
                    danceBodyYaw   = 0f;
                    break;
            }
        }

        // ── Normal locomotion / traversal face direction ──────────────────────
        if (model != null && controller != null && danceWeight < 0.6f)
        {
            Vector3 dir = controller.MoveDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0025f)
            {
                Quaternion want = Quaternion.LookRotation(dir.normalized, Vector3.up);
                model.rotation = Quaternion.Slerp(model.rotation, want, 1f - Mathf.Exp(-turnSpeed * dt));
            }
        }

        // Walk cycle phase
        if (speed > 0.12f && grounded)
            phase += dt * (5f + walkCycleSpeed * moveAmount);
        else if (grounded)
            phase = Mathf.Lerp(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, 10f * dt);

        float swing = Mathf.Sin(phase) * 42f * moveAmount;
        float walkArmL, walkArmR, walkLegL, walkLegR;

        if (grounded)
        {
            walkArmL = swing;
            walkArmR = -swing;
            walkLegL = -swing * 0.9f;
            walkLegR = swing * 0.9f;
        }
        else
        {
            walkArmL = -75f;
            walkArmR = -75f;
            walkLegL = 28f;
            walkLegR = -22f;
        }

        // ── Smooth blending between locomotion and dance ───────────────────────
        float targetArmL = Mathf.Lerp(walkArmL, danceArmLAngle, danceWeight);
        float targetArmR = Mathf.Lerp(walkArmR, danceArmRAngle, danceWeight);
        float targetArmLZ = Mathf.Lerp(0f, danceArmLZ, danceWeight);
        float targetArmRZ = Mathf.Lerp(0f, danceArmRZ, danceWeight);
        float targetLegL = Mathf.Lerp(walkLegL, danceLegLAngle, danceWeight);
        float targetLegR = Mathf.Lerp(walkLegR, danceLegRAngle, danceWeight);

        float limbBlend = 1f - Mathf.Exp(-16f * dt);

        if (armL != null)
            armL.localRotation = Quaternion.Slerp(armL.localRotation,
                armLBase * Quaternion.Euler(targetArmL, 0f, targetArmLZ), limbBlend);

        if (armR != null)
            armR.localRotation = Quaternion.Slerp(armR.localRotation,
                armRBase * Quaternion.Euler(targetArmR, 0f, targetArmRZ), limbBlend);

        if (legL != null)
            legL.localRotation = Quaternion.Slerp(legL.localRotation,
                legLBase * Quaternion.Euler(targetLegL, 0f, 0f), limbBlend);

        if (legR != null)
            legR.localRotation = Quaternion.Slerp(legR.localRotation,
                legRBase * Quaternion.Euler(targetLegR, 0f, 0f), limbBlend);

        if (torso != null)
        {
            Quaternion targetTorso = torsoBase * Quaternion.Euler(danceTorsoLean * danceWeight, danceTorsoYaw * danceWeight, 0f);
            torso.localRotation = Quaternion.Slerp(torso.localRotation, targetTorso, limbBlend);
        }

        if (head != null)
        {
            Quaternion targetHead = headBase * Quaternion.Euler(danceHeadNod * danceWeight, -danceTorsoYaw * 0.35f * danceWeight, danceHeadTilt * danceWeight);
            head.localRotation = Quaternion.Slerp(head.localRotation, targetHead, limbBlend);
        }

        if (model != null)
        {
            float walkBob = grounded ? Mathf.Abs(Mathf.Sin(phase)) * 0.055f * moveAmount : 0f;
            float totalBob = Mathf.Lerp(walkBob, danceHipBounce, danceWeight);
            model.localPosition = Vector3.Lerp(model.localPosition, modelBasePos + Vector3.up * totalBob, 14f * dt);

            if (danceWeight > 0.01f)
            {
                Quaternion danceModelRot = Quaternion.Euler(0f, danceBodyYaw * danceWeight, 0f);
                model.localRotation = Quaternion.Slerp(model.localRotation, danceModelRot, limbBlend);
            }

            if (squashTime  > 0f) squashTime  -= dt;
            if (jumpStretch > 0f) jumpStretch -= dt;

            float squash  = squashTime  > 0f ? (squashTime  / 0.18f) * 0.16f * squashStrength : 0f;
            float stretch = jumpStretch > 0f ? (jumpStretch / 0.20f) * 0.10f : 0f;
            Vector3 scale = new Vector3(1f + squash - stretch * 0.5f, 1f - squash + stretch, 1f + squash - stretch * 0.5f);
            model.localScale = Vector3.Lerp(model.localScale, scale, 16f * dt);
        }
    }

    public void PlayDeath()
    {
        dead    = true;
        dancing = false;
        falling = false;
        if (cc         != null) cc.enabled         = false;
        if (controller != null) controller.enabled = false;
    }
}
