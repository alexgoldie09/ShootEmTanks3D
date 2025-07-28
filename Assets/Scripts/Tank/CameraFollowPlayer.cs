/*
 * CameraFollowPlayer.cs
 * ----------------------------------------------------------------
 * A third-person camera script that follows the player using the custom math stack.
 *
 * PURPOSE:
 * - Maintain a dynamic offset behind and above the tank.
 * - Smoothly follow the tank using vector interpolation.
 * - Always look in the direction the tank is facing.
 *
 * FEATURES:
 * - Uses `Coords` for position and direction vectors.
 * - Interpolates movement using `MathEngine.Lerp()`.
 * - Extracts forward/up direction using custom math (not Unity directly).
 * - Executes in `LateUpdate()` to follow after movement logic.
 */

using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    [Header("References")]
    public Transform player;          // The player's transform (tank)

    [Header("Offset Settings")]
    public float distance = 5f;       // Distance behind the player along the forward vector
    public float height = 3f;         // Height above the player along the up vector

    [Header("Smoothing Settings")]
    public float smoothSpeed = 5f;    // Speed for smoothing movement using interpolation

    #region Unity Lifecycle
    void Start()
    {
        // Auto-assign player transform if not manually set
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;
        
        FollowPlayer();
    }
    #endregion

    #region Camera Logic
    // Calculates and applies the smoothed follow and look-at behavior.
    private void FollowPlayer()
    {
        // Convert player's position and direction vectors to custom Coords
        Coords playerPos = new Coords(player.position);
        Coords forward = MathEngine.Normalize(new Coords(player.forward));
        Coords up = MathEngine.Normalize(new Coords(player.up));

        // Calculate ideal camera position (behind and above player)
        Coords desiredPos = playerPos - forward * distance + up * height;

        // Smoothly interpolate from current to desired camera position
        Coords currentPos = new Coords(transform.position);
        Coords smoothedPos = MathEngine.Lerp(currentPos, desiredPos, smoothSpeed * Time.deltaTime);

        // Apply final position to Unity transform
        transform.position = smoothedPos.ToVector3();

        // Look slightly ahead in the player's forward direction
        transform.LookAt((playerPos + forward).ToVector3());
    }
    #endregion
}