/*
 * TankController.cs
 * ----------------------------------------------------------------
 * A movement controller for a 3D tank using a custom math and physics engine.
 *
 * PURPOSE:
 * - Handle forward/backward movement and Y-axis rotation (yaw) for a tank.
 * - Apply transformation using quaternion and matrix math from MathEngine.
 * - Sync custom math-driven transforms with Unity's Transform system.
 *
 * FEATURES:
 * - Reads user input (WASD) for movement and turning.
 * - Uses custom matrix math to compute and apply position/rotation.
 * - Applies final world transform using Coords and CustomQuaternion.
 * - Clean separation of movement input and transform application.
 */

using UnityEngine;
public class TankController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;       // Units per second
    public float rotateSpeed = 45f;    // Degrees per second

    // Internal state
    private Coords position = new Coords(0, 0, 0);  // Tank's world position
    private float yawDegrees = 0f;                  // Rotation around Y-axis

    #region Unity Lifecycle
    void Start()
    {
        // Initialize position from Unity transform
        position = new Coords(transform.position);
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        HandleInput(deltaTime);
        ApplyTransform();
    }
    #endregion

    #region Movement & Rotation
    // Reads input and applies movement + rotation logic using custom math.
    private void HandleInput(float deltaTime)
    {
        // W/S keys for forward/back
        float moveInput = Input.GetAxis("Vertical");

        // A/D keys for turning (yaw)
        float turnInput = Input.GetAxis("Horizontal");
        yawDegrees += turnInput * rotateSpeed * deltaTime;

        // Build rotation matrix from yaw
        Matrix yawMatrix = MathEngine.CreateRotationMatrixFromQuaternion(new Coords(0, 1, 0), yawDegrees);

        // Extract forward direction from rotation matrix (Z column)
        Coords forward = new Coords(
            yawMatrix.GetValue(0, 2),
            yawMatrix.GetValue(1, 2),
            yawMatrix.GetValue(2, 2)
        );

        // Apply movement along forward vector
        position += forward * (moveSpeed * moveInput * deltaTime);
    }
    #endregion

    #region Transform Application
    // Applies the final transform matrix and rotation to the Unity object.
    private void ApplyTransform()
    {
        // Build transform matrix (Translation * Rotation)
        Matrix translation = MathEngine.CreateTranslationMatrix(position);
        Matrix rotation = MathEngine.CreateRotationMatrixFromQuaternion(new Coords(0, 1, 0), yawDegrees);
        Matrix fullTransform = translation * rotation;

        // Extract final position and apply to Unity transform
        Coords finalPosition = MathEngine.ExtractPosition(fullTransform);
        transform.position = finalPosition.ToVector3();

        // Create and apply quaternion rotation to Unity transform
        CustomQuaternion rot = new CustomQuaternion(new Coords(0, 1, 0), yawDegrees);
        transform.rotation = rot.ToUnityQuaternion();
    }
    #endregion
}
