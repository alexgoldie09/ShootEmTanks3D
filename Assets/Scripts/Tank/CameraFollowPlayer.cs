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
    public Transform player;       // Target player's transform (tank)

    [Header("Offset Settings")]
    public float distance = 5f;    // Distance behind the player
    public float height = 3f;      // Height above the player

    [Header("Smoothing Settings")]
    public float smoothSpeed = 5f; // Interpolation speed for smooth camera motion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes references and auto-assigns the player transform if not set.
    /// </summary>
    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    /// <summary>
    /// Called after all Update() calls. Handles camera positioning and orientation.
    /// </summary>
    void LateUpdate()
    {
        if (player == null) return;
        FollowPlayer();
    }
    #endregion

    #region Camera Logic
    /// <summary>
    /// Calculates and applies the smoothed follow position and look-at behavior.
    /// </summary>
    private void FollowPlayer()
    {
        // Convert player's position and direction to Coords
        Coords playerPos = new Coords(player.position);
        Coords forward = MathEngine.Normalize(new Coords(player.forward));
        Coords up = MathEngine.Normalize(new Coords(player.up));

        // Calculate target camera position (behind and above the player)
        Coords desiredPos = playerPos - forward * distance + up * height;

        // Smoothly interpolate from current to target position
        Coords currentPos = new Coords(transform.position);
        Coords smoothedPos = MathEngine.Lerp(currentPos, desiredPos, smoothSpeed * Time.deltaTime);

        // Apply smoothed position to the Unity transform
        transform.position = smoothedPos.ToVector3();

        // Make the camera look ahead in the player's forward direction
        transform.LookAt((playerPos + forward).ToVector3());
    }
    #endregion
}
