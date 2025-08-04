using UnityEngine;

public class PhysicsBody : MonoBehaviour
{
    [Header("Physics Settings")]
    public float bounciness = 0.5f;
    public float restitutionThreshold = 0.2f;
    public bool useGravity = true;
    
    [Header("Projectile Settings")]
    public float projectileForce = 10f;
    public bool isProjectile = false;

    // Internal state
    private Coords position;
    private Coords velocity = Coords.Zero();
    private Coords acceleration = Coords.Zero();  // Rate of change of velocity

    private const float GRAVITY = -9.81f;

    #region Unity Lifecycle
    private void Start()
    {
        position = new Coords(transform.position);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // Reset acceleration at start of frame
        acceleration = Coords.Zero();

        // Apply gravity as acceleration
        if (useGravity)
            acceleration.y += GRAVITY;

        // Integrate acceleration → velocity
        velocity += acceleration * deltaTime;

        // Integrate velocity → position
        Matrix translation = MathEngine.CreateTranslationMatrix(velocity * deltaTime);
        Matrix newWorld = translation * MathEngine.CreateTranslationMatrix(position);

        position = MathEngine.ExtractPosition(newWorld);

        // Sync Unity transform
        transform.position = position.ToVector3();
        
        // Only orient if flagged as projectile
        if (isProjectile && MathEngine.Magnitude(velocity) > 0.01f)
        {
            CustomQuaternion rot = MathEngine.FromToRotation(new Coords(0, 0, 1), MathEngine.Normalize(velocity));
            transform.rotation = rot.ToUnityQuaternion();
        }
    }
    #endregion

    #region Collision Response Methods
    public void ResolveSphereCollision(Coords normal, float penetration, PhysicsBody other = null)
    {
        // Position correction (basic)
        if (penetration > 0f)
        {
            // Push this body out along the normal
            position += normal * penetration;
            transform.position = position.ToVector3();
        }

        // Velocity correction
        float velAlongNormal = MathEngine.Dot(velocity, normal);

        if (velAlongNormal < 0f) // moving into collision
        {
            if (bounciness > 0f)
            {
                // Reflect with restitution
                velocity = MathEngine.Reflect(velocity, normal) * bounciness;
            }
            else
            {
                // Kill velocity into the surface, keep tangential
                velocity -= normal * velAlongNormal;
            }
        }
    }


    public void ApplyImpulse(Coords impulse)
    {
        velocity += impulse;
    }
    
    // Stops a body on a ground surface with bounce & restitution.
    // Works for both spheres (halfHeight = radius) and AABBs (halfHeight = bounds.Extents.y).
    public void StopOnGround(Coords surfaceNormal, float groundHeight, float halfHeight = 0f)
    {
        // Reflect velocity based on surface normal (bounce)
        velocity = MathEngine.Reflect(velocity, surfaceNormal) * bounciness;

        // Apply restitution threshold (settle when very small)
        if (MathEngine.Magnitude(velocity) < restitutionThreshold)
        {
            velocity = Coords.Zero();
            acceleration = Coords.Zero();

            // Snap object exactly on ground
            position = new Coords(position.x, groundHeight + halfHeight, position.z);
        }
        else
        {
            // Prevent sinking below ground while still bouncing
            position = new Coords(position.x, groundHeight + halfHeight + 0.01f, position.z);
        }

        transform.position = position.ToVector3();
    }
    
    // Stops a body on a wall with bounce & restitution.
    // Works for both spheres (halfExtent = radius) and AABBs (halfExtent = bounds.Extents.x or z).
    public void StopOnWall(Coords surfaceNormal, float boundaryPos, char axis, float halfExtent = 0f)
    {
        // Reflect velocity (bounce)
        velocity = MathEngine.Reflect(velocity, surfaceNormal) * bounciness;

        // Apply restitution threshold (settle when very small)
        if (MathEngine.Magnitude(velocity) < restitutionThreshold)
        {
            velocity = Coords.Zero();
            acceleration = Coords.Zero();
        }

        // Correct position so object sits flush against wall
        if (axis == 'x')
            position = new Coords(boundaryPos + (surfaceNormal.x * halfExtent), position.y, position.z);
        else if (axis == 'z')
            position = new Coords(position.x, position.y, boundaryPos + (surfaceNormal.z * halfExtent));

        transform.position = position.ToVector3();
    }
    #endregion
    
    #region Velocity Accessors
    // Sets the current velocity of the body (overwrites existing velocity).
    public void SetVelocity(Coords newVelocity) => velocity = newVelocity;
    
    // Gets the current velocity of the body.
    public Coords GetVelocity() => velocity;
    #endregion
}