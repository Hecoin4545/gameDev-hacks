using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpHeight = 1.6f;
    public float gravity = -20f;
    public Transform cameraTransform;

    // Fall-off threshold: if the player drops below this Y, they respawn
    public float fallDeathY = -8f;

    private CharacterController cc;
    private Vector3 velocity;
    private Tile lastLandedTile;
    private bool inputEnabled = true;

    // Jump-enforcement: only count a tile as "landed" if the player was airborne first
    private bool prevWasGrounded = true;
    private bool justLandedFromAir = false;

    public bool IsGrounded { get; private set; }
    public Vector3 MoveDirection { get; private set; }

    void Awake() { cc = GetComponent<CharacterController>(); }

    public void SetInputEnabled(bool enabled) { inputEnabled = enabled; }

    // Read input supporting both legacy and new Input System.
    // Also handles Arrow keys alongside WASD.
    private float ReadAxis(string axisName, Key negKey, Key posKey, Key negArrow, Key posArrow)
    {
        float v = 0f;
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current[posKey].isPressed || Keyboard.current[posArrow].isPressed) v += 1f;
                if (Keyboard.current[negKey].isPressed || Keyboard.current[negArrow].isPressed) v -= 1f;
            }
        }
        catch { }
#endif
        if (Mathf.Approximately(v, 0f))
        {
            try { v = Input.GetAxisRaw(axisName); } catch { }
        }
        return v;
    }

    private bool ReadJump()
    {
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame)) return true;
        }
        catch { }
#endif
        try { if (Input.GetButtonDown("Jump")) return true; } catch { }
        return false;
    }

    private bool ReadDance()
    {
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) return true;
        }
        catch { }
#endif
        try { if (Input.GetKeyDown(KeyCode.F)) return true; } catch { }
        return false;
    }

    private bool ReadRestart()
    {
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) return true;
        }
        catch { }
#endif
        try { if (Input.GetKeyDown(KeyCode.R)) return true; } catch { }
        return false;
    }

    void Update()
    {
        if (!cc.enabled || !gameObject.activeInHierarchy) return;

        // Fall-death check: if player drops too far down, respawn at start
        if (transform.position.y < fallDeathY)
        {
            LevelManager.Instance?.RespawnAtStart();
            return;
        }

        IsGrounded = cc.isGrounded;

        // Detect the frame when the player first touches ground after being airborne.
        // Only in this frame do we count a tile as "landed" – walk-ons are ignored.
        justLandedFromAir = IsGrounded && !prevWasGrounded;

        if (IsGrounded && velocity.y < 0f) velocity.y = -2f;

        if (inputEnabled)
        {
            // WASD + Arrow keys
            float h = ReadAxis("Horizontal", Key.A, Key.D, Key.LeftArrow, Key.RightArrow);
            float v = ReadAxis("Vertical",   Key.S, Key.W, Key.DownArrow, Key.UpArrow);
            Vector3 move;
            if (cameraTransform != null)
            {
                Vector3 fwd = cameraTransform.forward; fwd.y = 0f; fwd.Normalize();
                Vector3 right = cameraTransform.right; right.y = 0f; right.Normalize();
                move = fwd * v + right * h;
            }
            else
            {
                move = new Vector3(h, 0f, v);
            }
            move = Vector3.ClampMagnitude(move, 1f);
            MoveDirection = move;
            cc.Move(move * moveSpeed * Time.deltaTime);

            if (ReadJump() && IsGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Dance on F key (only when grounded and not mid-air)
            if (ReadDance() && IsGrounded)
            {
                var visual = GetComponent<PlayerVisual>();
                if (visual != null) visual.TriggerDance();
            }

            if (ReadRestart())
            {
                LevelManager.Instance?.RestartLevel();
            }

            if (ReadMenu())
            {
                LevelManager.Instance?.LoadMainMenu();
            }
        }
        else
        {
            MoveDirection = Vector3.zero;
        }

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);

        // Record grounded state for next frame's jump-detection
        prevWasGrounded = IsGrounded;

        if (!IsGrounded) lastLandedTile = null;
    }

    private bool ReadMenu()
    {
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
        }
        catch { }
#endif
        try { if (Input.GetKeyDown(KeyCode.Escape)) return true; } catch { }
        return false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsGrounded) return;
        // *** JUMP ENFORCEMENT: only register a tile if the player jumped onto it ***
        // justLandedFromAir is true only on the single frame the player transitions
        // from airborne → grounded, so walking along tiles never counts.
        if (!justLandedFromAir) return;
        if (hit.normal.y < 0.5f) return;
        var tile = hit.collider.GetComponent<Tile>();
        if (tile == null) return;
        if (tile == lastLandedTile) return;
        lastLandedTile = tile;
        LevelManager.Instance?.OnTileLanded(tile);
    }
}
