/*
 * TankController.cs
 * ----------------------------------------------------------------
 * Controls movement, rotation, and firing logic for a 3D tank using a custom math and physics engine.
 *
 * PURPOSE:
 * - Handle forward/backward movement and yaw (Y-axis) rotation.
 * - Adjust and fire projectiles with configurable force.
 * - Apply movement and aiming using custom matrix and quaternion math.
 * - Integrate with collision and custom physics systems.
 *
 * FEATURES:
 * - Custom movement and rotation using `MathEngine`.
 * - Collision-bound movement clamping via `CollisionEngine`.
 * - Adjustable firing force via mouse scroll input.
 * - Projectile instantiation with `PhysicsBody` velocity assignment.
 * - Destroy effects support.
 */

using UnityEngine;

public class TankController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;            // Units per second
    public float rotateSpeed = 45f;         // Degrees per second

    [Header("Weapon Settings")]
    public GameObject shellPrefab;          // Prefab for fired projectile
    public float fireForce = 20f;           // Initial projectile speed
    public float minForce = 10f;            // Minimum firing force
    public float maxForce = 50f;            // Maximum firing force
    public float forceAdjustSpeed = 10f;    // Speed of force adjustment
    public Transform firePoint;             // Barrel end position

    [Header("Destroy Effects")]
    public GameObject destroyFx;            // Explosion effect prefab

    // Internal state
    private Coords position = new Coords(0, 0, 0); // Tank's world position
    private float yawDegrees = 0f;                 // Rotation around Y-axis

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the tank's starting position.
    /// </summary>
    void Start()
    {
        position = new Coords(transform.position);
    }

    /// <summary>
    /// Main update loop for movement, shooting, and input handling.
    /// </summary>
    void Update()
    {
        if (!GameManager.Instance.gameOver)
        {
            float deltaTime = Time.deltaTime;

            HandleInput(deltaTime);
            HandleFireForceAdjustment(deltaTime);
            HandleShooting();
            ApplyTransform();
        }
    }
    #endregion

    #region Movement & Rotation
    /// <summary>
    /// Handles user input for movement and rotation, applying collision clamping.
    /// </summary>
    private void HandleInput(float deltaTime)
    {
        // W/S for forward/back movement
        float moveInput = Input.GetAxis("Vertical");

        // A/D for yaw rotation
        float turnInput = Input.GetAxis("Horizontal");
        yawDegrees += turnInput * rotateSpeed * deltaTime;

        // Get forward direction from tank's yaw
        Matrix yawMatrix = MathEngine.CreateRotationYDegrees(yawDegrees);
        
        // Extract forward (Z column)
        Coords forward = new Coords(
            yawMatrix.GetValue(0, 2),
            yawMatrix.GetValue(1, 2),
            yawMatrix.GetValue(2, 2)
        );

        // Proposed movement
        Coords proposedMove = forward * (moveSpeed * moveInput * deltaTime);
        Coords proposedPos = position + proposedMove;

        // Clamp position against collision bounds
        CustomCollider col = GetComponent<CustomCollider>();
        if (col != null && CollisionEngine.Instance != null)
        {
            proposedPos = CollisionEngine.Instance.ClampToBounds(col, proposedPos);
        }

        // Apply final movement
        position = proposedPos;
    }

    /// <summary>
    /// Applies the calculated position and rotation to the Unity transform.
    /// </summary>
    private void ApplyTransform()
    {
        // Build transformation matrix
        Matrix translation = MathEngine.CreateTranslationMatrix(position);
        Matrix rotation = MathEngine.CreateRotationYDegrees(yawDegrees);
        Matrix fullTransform = translation * rotation;

        // Apply position to transform
        Coords finalPosition = MathEngine.ExtractPosition(fullTransform);
        transform.position = finalPosition.ToVector3();

        // Apply rotation from matrix
        CustomQuaternion rot = MathEngine.FromMatrix(fullTransform);
        transform.rotation = rot.ToUnityQuaternion();
    }
    #endregion

    #region Shooting
    /// <summary>
    /// Adjusts the firing force using the mouse scroll wheel.
    /// </summary>
    private void HandleFireForceAdjustment(float deltaTime)
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0f)
        {
            fireForce += scroll * forceAdjustSpeed;
            fireForce = Mathf.Clamp(fireForce, minForce, maxForce);
        }
    }

    /// <summary>
    /// Handles projectile firing when the spacebar is pressed.
    /// </summary>
    private void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.Space) && shellPrefab != null)
        {
            // Determine projectile spawn position
            Coords spawnPos = firePoint != null ? new Coords(firePoint.position) : position;

            // Get forward direction from tank's yaw
            Matrix yawMatrix = MathEngine.CreateRotationYDegrees(yawDegrees);
            // Extract forward (Z column)
            Coords forward = new Coords(
                yawMatrix.GetValue(0, 2),
                yawMatrix.GetValue(1, 2),
                yawMatrix.GetValue(2, 2)
            );

            // Calculate rotation to face forward
            CustomQuaternion shellRot = MathEngine.FromToRotation(new Coords(0, 0, 1), forward);

            // Spawn projectile
            GameObject shellObj = Instantiate(shellPrefab, spawnPos.ToVector3(), shellRot.ToUnityQuaternion());

            // Apply projectile velocity
            PhysicsBody body = shellObj.GetComponent<PhysicsBody>();
            if (body != null)
            {
                body.SetVelocity(forward * fireForce);
            }
        }
    }
    #endregion
}