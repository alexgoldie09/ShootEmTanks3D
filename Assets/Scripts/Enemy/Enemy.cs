/*
 * Enemy.cs
 * ----------------------------------------------------------------
 * An enemy behavior script using a custom physics system to patrol and chase.
 *
 * PURPOSE:
 * - Allow enemies to patrol between fixed points or chase the player.
 * - Apply physics-based steering impulse toward the player in chase mode.
 * - Return to patrol if player is lost or out of range.
 * - Manage enemy health and flashing damage feedback.
 *
 * FEATURES:
 * - Uses custom raycasting and distance check to detect player.
 * - Smooth physics-based movement using impulse and velocity.
 * - Patrols through waypoints or chases player with bounce impulse.
 * - Flashes red when hit by a projectile.
 * - Debug visualization for detection direction and range.
 */

using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public enum AIState { Patrol, Chase }

    [Header("General Settings")]
    public AIState currentState = AIState.Patrol;     // Current AI state (patrolling or chasing)
    public Transform player;                          // Reference to the player
    public float detectionRange = 10f;                // Max distance for detecting player
    public float rayOffsetHeight = 0.5f;              // Height offset for ray origin (eye height)
    public float bounceForce = 2f;                    // Bounce force added when chasing player

    [Header("Chase Settings")]
    public float maxSpeed = 5f;                       // Max speed for chasing
    public float steeringForce = 10f;                 // Steering velocity multiplier

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;                  // Array of points to patrol between
    public float patrolSpeed = 8f;                    // Speed of movement during patrol
    private int currentPatrolIndex = 0;               // Index of the current patrol target

    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;
    public Renderer rend;
    public Color hitColor = Color.red;
    private Color originalColor;
    public float flashDuration = 0.15f;

    private PhysicsBody body;                         // Reference to this object's physics controller
    private CustomCollider myCollider;                // Reference to this object's collider
    private CustomCollider playerCollider;            // Reference to the player collider

    #region Unity Lifecycle
    void Start()
    {
        body = GetComponent<PhysicsBody>();
        myCollider = GetComponent<CustomCollider>();
        currentHealth = maxHealth;

        if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (playerCollider == null && player != null)
        {
            playerCollider = player.gameObject.GetComponent<CustomCollider>();
        }

        if (rend == null) rend = GetComponent<Renderer>();
        if (rend != null) originalColor = rend.material.color;
    }

    void Update()
    {
        if (!GameManager.Instance.gameOver)
        {
            // Act based on current state
            switch (currentState)
            {
                case AIState.Chase:
                    HandleChase();
                    break;
                case AIState.Patrol:
                    HandlePatrol();
                    break;
            }

            CheckForPlayer();
        }
    }
    #endregion

    #region AI Behaviour
    // Handles logic for chasing using smooth steering towards player with upward impulse.
    private void HandleChase()
    {
        if (player == null || body == null) return;

        Coords origin = new Coords(transform.position + new Vector3(0, rayOffsetHeight, 0));
        Coords target = new Coords(player.position);
        Coords direction = target - origin;

        // Debug: Red line toward player
        Debug.DrawLine(origin.ToVector3(), target.ToVector3(), Color.red);

        // Smooth physics-based steering
        Coords desiredVelocity = MathEngine.Normalize(direction) * maxSpeed;
        Coords steering = desiredVelocity - body.GetVelocity();

        // Add upward bump for bounce effect
        Coords upward = new Coords(0f, 1f, 0f);
        Coords bounceSteering = steering + upward * bounceForce;

        body.ApplyImpulse(bounceSteering * steeringForce * Time.deltaTime);
    }

    // Handles patrolling between points at a fixed speed.
    private void HandlePatrol()
    {
        if (patrolPoints.Length == 0 || body == null) return;

        // Get current and target patrol positions
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Coords currentPos = new Coords(transform.position);
        Coords targetPos = new Coords(targetPoint.position);

        // Move toward the patrol point
        Coords direction = MathEngine.Normalize(targetPos - currentPos);
        body.ApplyImpulse(direction * patrolSpeed * Time.deltaTime);

        // If close to the patrol point, advance to the next one
        if (MathEngine.Distance(currentPos, targetPos) < 1f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    // Check if player is visible and within range to trigger chase.
    private void CheckForPlayer()
    {
        if (player == null || CollisionEngine.Instance == null) return;

        // Ray origin from enemy's eye height
        Coords origin = new Coords(transform.position + new Vector3(0, rayOffsetHeight, 0));
        Coords target = new Coords(player.position + new Vector3(0, playerCollider.playerOffsetY, 0));
        Coords dir = MathEngine.Normalize(target - origin);
        float distance = MathEngine.Distance(origin, target);

        // Raycast only against the PLAYER collider type
        Debug.DrawLine(origin.ToVector3(), (origin + dir * detectionRange).ToVector3(), Color.magenta);

        if (distance <= detectionRange)
        {
            // Raycast with no filter so we can check what is hit first
            if (CollisionEngine.Instance.Raycast(origin, dir, out var hit, distance, null, myCollider))
            {
                if (hit.colliderType == CustomCollider.ColliderType.PLAYER)
                {
                    currentState = AIState.Chase;
                }
                else
                {
                    if (currentState == AIState.Chase)
                    {
                        currentState = AIState.Patrol;
                    }
                }
            }
            else
            {
                // No hit at all
                if (currentState == AIState.Chase)
                {
                    currentState = AIState.Patrol;
                }
            }
        }
    }
    #endregion

    #region Health System
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (rend != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashColor());
        }
    }

    private IEnumerator FlashColor()
    {
        rend.material.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        rend.material.color = originalColor;
    }
    #endregion

    #region Debugging Methods
    // Draw detection radius in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
    #endregion
}
