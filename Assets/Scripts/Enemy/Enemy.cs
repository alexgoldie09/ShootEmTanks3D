/*
 * Enemy.cs
 * ----------------------------------------------------------------
 * An enemy behavior script using a custom physics system to patrol and chase.
 *
 * PURPOSE:
 * - Allow enemies to patrol between fixed points or chase the player.
 * - Apply physics-based steering impulses toward the player in chase mode.
 * - Return to patrol if the player is lost or out of range.
 * - Manage enemy health and flashing damage feedback.
 *
 * FEATURES:
 * - Uses custom raycasting and distance checks to detect the player.
 * - Smooth physics-based movement via impulses and velocity steering.
 * - Patrols through waypoints or chases player with an upward "bounce" kick.
 * - Flashes red when hit by a projectile; spawns FX on death.
 * - Scene gizmo for detection radius debug.
 */

using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public enum AIState { Patrol, Chase }

    [Header("General Settings")]
    public AIState currentState = AIState.Patrol;  // Current AI state
    public Transform player;                       // Player transform
    public float detectionRange = 10f;             // Max distance to detect player
    public float rayOffsetHeight = 0.5f;           // Eye height offset for LOS checks
    public float bounceForce = 2f;                 // Upward kick while chasing

    [Header("Chase Settings")]
    public float maxSpeed = 5f;                    // Desired chase speed
    public float steeringForce = 10f;              // Impulse scaling for steering
    public GameObject chaseMarker;                 // Optional indicator shown while chasing

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;               // Patrol waypoints
    public float patrolSpeed = 8f;                 // Patrol impulse strength
    private int currentPatrolIndex = 0;            // Which waypoint we’re moving toward

    [Header("Detection Tuning")]
    public float chaseLoseDelay = 0.35f;           // Grace period before giving up chase
    private float chaseLoseTimer = 0f;             // Accumulator for lose delay

    [Header("Anti-Stuck")]
    public float stuckSpeedThreshold = 0.05f;      // Below this planar speed → “stuck”
    public float stuckTime = 0.6f;                 // Time below threshold before nudge
    public float unstickImpulse = 2.0f;            // Sideways kick magnitude
    private float stuckTimer = 0f;                 // Accumulator for stuck detection

    [Header("Health Settings")]
    public int maxHealth = 3;                      // Max HP
    private int currentHealth;                     // Current HP
    public Renderer rend;                          // Renderer for damage flash
    public Color hitColor = Color.red;             // Flash color
    private Color originalColor;                   // Cached base color
    public float flashDuration = 0.15f;            // Flash lifetime (seconds)
    public GameObject destroyFx;                   // Death VFX prefab

    // Cached references
    private PhysicsBody body;                      // Physics controller
    private CustomCollider myCollider;             // Enemy collider
    private CustomCollider playerCollider;         // Player collider (for eye height)

    #region Unity Lifecycle
    /// <summary>
    /// Initialize references, state, and visuals.
    /// </summary>
    void Start()
    {
        body = GetComponent<PhysicsBody>();
        myCollider = GetComponent<CustomCollider>();
        currentHealth = maxHealth;

        // Auto-assign player and player collider if not set
        if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        if (playerCollider == null && player != null)
            playerCollider = player.gameObject.GetComponent<CustomCollider>();

        // Renderer setup for damage flash
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend != null) originalColor = rend.material.color;

        // Hide chase marker initially
        if (chaseMarker != null) chaseMarker.SetActive(false);
    }

    /// <summary>
    /// Per-frame AI: run state behavior then check detection/transition.
    /// </summary>
    void Update()
    {
        if (GameManager.Instance.gameOver) return;

        switch (currentState)
        {
            case AIState.Chase:  HandleChase();  break;
            case AIState.Patrol: HandlePatrol(); break;
        }

        CheckForPlayer();
    }
    #endregion

    #region AI Behaviour
    /// <summary>
    /// Chasing: steer toward the player with an upward “bounce” component.
    /// </summary>
    private void HandleChase()
    {
        if (player == null || body == null) return;

        // LOS origin at "eye height"
        Coords origin    = new Coords(transform.position + new Vector3(0, rayOffsetHeight, 0));
        Coords target    = new Coords(player.position);
        Coords toPlayer  = target - origin;

        // Debug ray to player
        Debug.DrawLine(origin.ToVector3(), target.ToVector3(), Color.red);

        // Steering: desired - current
        Coords desiredVelocity = MathEngine.Normalize(toPlayer) * maxSpeed;
        Coords steering        = desiredVelocity - body.GetVelocity();

        // Upward kick for bouncy movement
        Coords bounceSteering  = steering + new Coords(0f, 1f, 0f) * bounceForce;

        // Apply as impulse (frame-scaled)
        body.ApplyImpulse(bounceSteering * steeringForce * Time.deltaTime);

        // Nudge if stuck
        AntiStuckNudge(toPlayer);
    }

    /// <summary>
    /// Patrolling: impulse toward current waypoint; advance when close.
    /// </summary>
    private void HandlePatrol()
    {
        if (patrolPoints.Length == 0 || body == null) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Coords currentPos     = new Coords(transform.position);
        Coords targetPos      = new Coords(targetPoint.position);

        // Move toward the patrol point
        Coords dir = MathEngine.Normalize(targetPos - currentPos);
        body.ApplyImpulse(dir * patrolSpeed * Time.deltaTime);

        // Nudge if stuck while trying to reach the point
        AntiStuckNudge(targetPos - currentPos);

        // Step to next when close enough
        if (MathEngine.Distance(currentPos, targetPos) < 1f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    /// <summary>
    /// Detects the player using a custom raycast; switches states with hysteresis.
    /// </summary>
    private void CheckForPlayer()
    {
        if (player == null || CollisionEngine.Instance == null) return;

        // Eye-height origin + adjusted player target (use player collider’s Y offset if present)
        Coords origin = new Coords(transform.position + new Vector3(0, rayOffsetHeight, 0));
        float playerEyeY = (playerCollider != null) ? playerCollider.playerOffsetY : 0f;
        Coords target = new Coords(player.position + new Vector3(0, playerEyeY, 0));

        Coords dir      = MathEngine.Normalize(target - origin);
        float distance  = MathEngine.Distance(origin, target);
        bool  inRange   = distance <= detectionRange;

        // Do a line-of-sight raycast to the player
        bool hasLOS = false;
        if (CollisionEngine.Instance.Raycast(origin, dir, out CustomCollider hit, distance, null, myCollider))
            hasLOS = (hit != null && hit.colliderType == CustomCollider.ColliderType.PLAYER);

        // Enter/exit chase with a small lose delay (prevents flicker)
        if (inRange && hasLOS)
        {
            chaseLoseTimer = 0f;
            if (currentState != AIState.Chase)
            {
                currentState = AIState.Chase;
                if (chaseMarker != null) chaseMarker.SetActive(true);
            }
        }
        else
        {
            if (currentState == AIState.Chase)
            {
                chaseLoseTimer += Time.deltaTime;
                if (chaseLoseTimer >= chaseLoseDelay)
                {
                    currentState = AIState.Patrol;
                    if (chaseMarker != null) chaseMarker.SetActive(false);
                }
            }
            else
            {
                chaseLoseTimer = 0f; // already patrolling
            }
        }
    }

    /// <summary>
    /// If planar speed is too low for a while, push sideways + slightly upward to unstick.
    /// </summary>
    private void AntiStuckNudge(Coords desiredDir)
    {
        if (body == null) return;

        // Planar speed (XZ)
        Coords v = body.GetVelocity();
        float planarSpeed = Mathf.Sqrt(v.x * v.x + v.z * v.z);

        if (planarSpeed < stuckSpeedThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTime)
            {
                // Sideways vector in XZ, orthogonal to desired direction
                Coords forward = new Coords(desiredDir.x, 0f, desiredDir.z);
                if (MathEngine.Magnitude(forward) < 0.001f) forward = new Coords(0f, 0f, 1f);

                Coords right = new Coords(-forward.z, 0f, forward.x); // 90° right in XZ
                right = MathEngine.Normalize(right);

                // Small nudge to break friction/stiction
                Coords nudge = right * unstickImpulse + new Coords(0f, 0.2f, 0f);
                body.ApplyImpulse(nudge);

                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }
    #endregion

    #region Health System
    /// <summary>
    /// Apply damage; flash on hit; destroy on zero HP with FX and score.
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            if (destroyFx != null)
            {
                GameObject fx = Instantiate(destroyFx, transform.position, Quaternion.identity);
                Destroy(fx, 3f);
            }

            GameManager.Instance.AddScore(3, true);
            Destroy(gameObject);
            return;
        }

        if (rend != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashColor());
        }
    }

    /// <summary>
    /// Briefly tint to hitColor, then restore originalColor.
    /// </summary>
    private IEnumerator FlashColor()
    {
        rend.material.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        rend.material.color = originalColor;
    }
    #endregion

    #region Debugging
    /// <summary>
    /// Draw detection range in the Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
    #endregion
}