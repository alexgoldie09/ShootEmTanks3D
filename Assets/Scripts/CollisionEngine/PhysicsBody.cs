/*
 * PhysicsBody.cs
 * ----------------------------------------------------------------
 * Represents a physics-enabled object in the custom physics system.
 *
 * PURPOSE:
 * - Handle basic motion integration (position, velocity, acceleration).
 * - Apply gravity and projectile orientation.
 * - Respond to collisions with ground, walls, and other physics bodies.
 *
 * FEATURES:
 * - Uses Coords and MathEngine for all math operations.
 * - Supports gravity toggle and bounciness.
 * - Includes sphere collision resolution, impulse application, and stop methods for ground/walls.
 * - Optionally rotates projectiles to face their velocity vector.
 */

using UnityEngine;

public class PhysicsBody : MonoBehaviour
{
    [Header("Physics Settings")]
    public float bounciness = 0.5f;           // Bounce factor after collisions
    public float restitutionThreshold = 0.2f; // Velocity threshold to stop bouncing
    public bool useGravity = true;            // Apply gravity if true

    [Header("Projectile Settings")]
    public float projectileForce = 10f;       // Initial force for projectiles
    public bool isProjectile = false;         // Rotate to face velocity if true

    // Internal state
    private Coords position;                  // Current position in world space
    private Coords velocity = Coords.Zero();  // Current velocity
    private Coords acceleration = Coords.Zero(); // Current acceleration (rate of velocity change)

    // Constant gravity acceleration
    private const float GRAVITY = -9.81f;

    #region Unity Lifecycle
    private void Start()
    {
        // Cache initial position
        position = new Coords(transform.position);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // Reset acceleration each frame
        acceleration = Coords.Zero();

        // Apply gravity
        if (useGravity)
            acceleration.y += GRAVITY;

        // Integrate acceleration → velocity
        velocity += acceleration * deltaTime;

        // Integrate velocity → position
        Matrix translation = MathEngine.CreateTranslationMatrix(velocity * deltaTime);
        Matrix newWorld = translation * MathEngine.CreateTranslationMatrix(position);
        position = MathEngine.ExtractPosition(newWorld);

        // Apply to Unity transform
        transform.position = position.ToVector3();

        // If projectile, orient in direction of velocity
        if (isProjectile && MathEngine.Magnitude(velocity) > 0.01f)
        {
            CustomQuaternion rot = MathEngine.FromToRotation(
                new Coords(0, 0, 1), 
                MathEngine.Normalize(velocity)
            );
            transform.rotation = rot.ToUnityQuaternion();
        }
    }
    #endregion

    #region Collision Response Methods
    /// <summary>
    /// Resolves a sphere collision by separating and reflecting velocity.
    /// </summary>
    public void ResolveSphereCollision(Coords normal, float penetration, PhysicsBody other = null)
    {
        // Correct position to remove penetration
        if (penetration > 0f)
        {
            position += normal * penetration;
            transform.position = position.ToVector3();
        }

        // Project velocity onto collision normal
        float velAlongNormal = MathEngine.Dot(velocity, normal);

        // If moving into collision, adjust velocity
        if (velAlongNormal < 0f)
        {
            if (bounciness > 0f)
                velocity = MathEngine.Reflect(velocity, normal) * bounciness; // Bounce
            else
                velocity -= normal * velAlongNormal; // Remove normal component (slide)
        }
    }

    /// <summary>
    /// Adds an instantaneous change in velocity.
    /// </summary>
    public void ApplyImpulse(Coords impulse)
    {
        velocity += impulse;
    }

    /// <summary>
    /// Stops and optionally bounces object on ground.
    /// </summary>
    public void StopOnGround(Coords surfaceNormal, float groundHeight, float halfHeight = 0f)
    {
        // Bounce off surface
        velocity = MathEngine.Reflect(velocity, surfaceNormal) * bounciness;

        if (MathEngine.Magnitude(velocity) < restitutionThreshold)
        {
            // Stop completely and snap to ground
            velocity = Coords.Zero();
            acceleration = Coords.Zero();
            position = new Coords(position.x, groundHeight + halfHeight, position.z);
        }
        else
        {
            // Prevent sinking while still bouncing
            position = new Coords(position.x, groundHeight + halfHeight + 0.01f, position.z);
        }

        transform.position = position.ToVector3();
    }

    /// <summary>
    /// Stops and optionally bounces object on a wall.
    /// </summary>
    public void StopOnWall(Coords surfaceNormal, float boundaryPos, char axis, float halfExtent = 0f)
    {
        velocity = MathEngine.Reflect(velocity, surfaceNormal) * bounciness;

        if (MathEngine.Magnitude(velocity) < restitutionThreshold)
        {
            velocity = Coords.Zero();
            acceleration = Coords.Zero();
        }

        // Snap flush to wall depending on axis
        if (axis == 'x')
            position = new Coords(boundaryPos + (surfaceNormal.x * halfExtent), position.y, position.z);
        else if (axis == 'z')
            position = new Coords(position.x, position.y, boundaryPos + (surfaceNormal.z * halfExtent));

        transform.position = position.ToVector3();
    }
    #endregion

    #region Accessors & Mutators
    public void SetVelocity(Coords newVelocity) => velocity = newVelocity; // Overwrites velocity
    public Coords GetVelocity() => velocity;                               // Gets velocity
    #endregion
}
