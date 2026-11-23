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

    private CharacterController cc;
    private Vector3 velocity;
    private Tile lastLandedTile;

    public bool IsGrounded { get; private set; }
    public Vector3 MoveDirection { get; private set; }

    void Awake() { cc = GetComponent<CharacterController>(); }

    // Read input supporting both legacy and new Input System
    private float ReadAxis(string axisName, Key negKey, Key posKey)
    {
        float v = 0f;
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current[posKey].isPressed) v += 1f;
                if (Keyboard.current[negKey].isPressed) v -= 1f;
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
        IsGrounded = cc.isGrounded;
        if (IsGrounded && velocity.y < 0f) velocity.y = -2f;

        float h = ReadAxis("Horizontal", Key.A, Key.D);
        float v = ReadAxis("Vertical", Key.S, Key.W);
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
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);

        if (!IsGrounded) lastLandedTile = null;

        if (ReadRestart())
        {
            LevelManager.Instance?.RestartLevel();
        }

        if (ReadMenu())
        {
            LevelManager.Instance?.LoadMainMenu();
        }
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
        if (hit.normal.y < 0.5f) return;
        var tile = hit.collider.GetComponent<Tile>();
        if (tile == null) return;
        if (tile == lastLandedTile) return;
        lastLandedTile = tile;
        LevelManager.Instance?.OnTileLanded(tile);
    }
}
