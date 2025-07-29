using UnityEngine;

public class PhysicsBody : MonoBehaviour
{
    // Settings
    public float bounciness = 0.5f;   // for later
    public float restitutionThreshold = 0.2f;
    public bool useGravity = true;

    // Internal state
    private Coords position;
    private Coords velocity = Coords.Zero();
    private Coords acceleration = Coords.Zero();  // Rate of change of velocity

    private const float GRAVITY = -9.81f;

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
    }

    // For collisions later
    public void ReflectFromNormal(Coords normal)
    {
        velocity = MathEngine.Reflect(velocity, normal) * bounciness;

        // Stop if velocity is too small
        if (MathEngine.Magnitude(velocity) < 0.01f)
        {
            velocity = Coords.Zero();
            acceleration = Coords.Zero();
            useGravity = false;
        }
    }
    
    public void ApplyImpulse(Coords impulse)
    {
        velocity += impulse;
        useGravity = true;
    }

    public void StopOnGround(Coords surfaceNormal, float groundHeight)
    {
        // Reflect velocity based on surface normal (bounce)
        velocity = MathEngine.Reflect(velocity, surfaceNormal) * bounciness;

        // Apply restitution threshold (settle when very small)
        if (MathEngine.Magnitude(velocity) < restitutionThreshold)
        {
            velocity = Coords.Zero();
            acceleration = Coords.Zero();
            position = new Coords(position.x, groundHeight, position.z);
        }
        else
        {
            // Prevent sinking below ground
            position = new Coords(position.x, groundHeight + 0.01f, position.z);
        }

        transform.position = position.ToVector3();
    }
    
    public void StopOnWall(Coords surfaceNormal, float boundaryPos, char axis)
    {
        // Reflect velocity (bounce)
        velocity = MathEngine.Reflect(velocity, surfaceNormal) * bounciness;

        // Settle if very slow
        if (MathEngine.Magnitude(velocity) < restitutionThreshold)
        {
            velocity = Coords.Zero();
            acceleration = Coords.Zero();
        }

        // Correct position so we don’t sink into the wall
        if (axis == 'x')
            position = new Coords(boundaryPos, position.y, position.z);
        else if (axis == 'z')
            position = new Coords(position.x, position.y, boundaryPos);

        transform.position = position.ToVector3();
    }
}