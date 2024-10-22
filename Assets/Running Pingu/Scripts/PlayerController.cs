using System.Collections;
using UnityEngine;

// Enum to define movement directions
public enum MovementDirection
{
    Left,
    Right
}

// Enum to define different player movement states
public enum MovementState
{
    Idle,
    Running,
    Sliding,
    Airborne
}

public class PlayerController : MonoBehaviour
{
    // Constants for lane distance and animation parameters
    private const float LANE_DISTANCE = 3.0f;
    private static readonly int ANIM_GROUNDED = Animator.StringToHash("IsGrounded");
    private static readonly int ANIM_RUNNING = Animator.StringToHash("Running");
    private static readonly int ANIM_SLIDING = Animator.StringToHash("Sliding");
    private static readonly int ANIM_JUMP = Animator.StringToHash("Jump");

    // Player-related components and references, to be assigned in the Unity Inspector
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private CharacterController controller;
    [SerializeField] private TrailRenderer trail;

    // Configuration for player settings like gravity and look rotation speed
    [Header("Settings")]
    [SerializeField] private float gravity = 12f;
    [SerializeField] private float lookRotationDuration = 0.05f;

    // Audio settings for sliding and swiping sounds
    [Header("Sounds")]
    [SerializeField] private AudioSource slideSource;
    [SerializeField] private AudioClip swipeSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private float swipePitchVariation = 0.1f;
    [SerializeField] private float slidePitchVariation = 0.1f;

    // Speed and movement parameters
    [Header("Speed")]
    [SerializeField] private float baseSpeed = 7f;

    // Jump parameters
    [Header("Jumping")]
    [SerializeField] private float jumpForce = 4f;

    // Slide parameters and hitbox modification during sliding
    [Header("Sliding")]
    [SerializeField] private float slideDuration = 1f;
    [SerializeField] private float slideHitboxMutliplier = 0.5f;

    // Ground detection parameters using raycasting
    [Header("Grounded")]
    [SerializeField] private LayerMask groundedLayerMask;
    [SerializeField] private float groundedRayOffsetY = 0.2f;
    [SerializeField] private float groundedRayTreshold = 0.1f;

    // Player state variables
    private MovementState movementState = MovementState.Idle;
    private int desiredLane = 1; // 0 = left, 1 = middle, 2 = right
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private float verticalVelocity;
    private float startingHitboxHeight;
    private Vector3 startingHitboxCenter;
    private float speed;
    private Coroutine slideRoutine;

    // Public accessors for movement state, grounded state, and speed
    public bool IsGrounded => isGrounded;
    public float Speed => speed;
    public MovementState MovementState => movementState;

    // Initial setup
    private void Start()
    {
        // Store the original size of the CharacterController hitbox
        startingHitboxHeight = controller.height;
        startingHitboxCenter = controller.center;

        // Set initial state to Idle
        Idle();
    }

    // Set player to idle state
    public void Idle()
    {
        movementState = MovementState.Idle;
        isGrounded = true;
        wasGroundedLastFrame = true;
        desiredLane = 1; // Middle lane by default
    }

    // Set player to running state
    public void StartRunning()
    {
        movementState = MovementState.Running;
    }

    // Stop running and reset to idle state
    public void StopRunning()
    {
        Idle();
    }

    // Update is called once per frame
    private void Update()
    {
        // Return if player is not in the running state
        if (player.State != PlayerState.Running)
            return;

        // Calculate speed based on game difficulty
        speed = baseSpeed * GameManager.Instance.DifficultyModifier;

        // Check if the player is grounded
        wasGroundedLastFrame = isGrounded;
        isGrounded = GroundedRaycast();

        // Handle player input and movement
        HandleInput();
        Move();
        HandleRotation();
        HandleTrailDisplay();

        // Update animation states based on movement
        UpdateAnimations();
    }

    // Handle player input for lane switching, jumping, sliding
    private void HandleInput()
    {
        if (MobileInput.Instance.SwipeLeft)
        {
            // Play swipe sound and move left
            AudioManager.Instance.PlaySound2DOneShot(swipeSound, pitchVariation: swipePitchVariation);
            MoveLane(MovementDirection.Left);
        }
        else if (MobileInput.Instance.SwipeRight)
        {
            // Play swipe sound and move right
            AudioManager.Instance.PlaySound2DOneShot(swipeSound, pitchVariation: swipePitchVariation);
            MoveLane(MovementDirection.Right);
        }

        // Handle landing
        if (!wasGroundedLastFrame && isGrounded)
        {
            Land();
        }

        // Jump or slide when grounded
        if (isGrounded)
        {
            if (MobileInput.Instance.SwipeUp)
            {
                if (movementState == MovementState.Sliding)
                    CancelSlide(); // Cancel sliding if in sliding state

                Jump();
            }
            else if (MobileInput.Instance.SwipeDown)
            {
                if (movementState != MovementState.Sliding)
                {
                    if (slideRoutine != null)
                        StopCoroutine(slideRoutine);
                    slideRoutine = StartCoroutine(Slide());
                }
            }
        }
        else
        {
            // Handle fast fall in the air
            if (MobileInput.Instance.SwipeDown)
            {
                FastFall();
            }
        }
    }

    // Handle player jump
    private void Jump()
    {
        isGrounded = false;
        movementState = MovementState.Airborne;
        verticalVelocity = jumpForce;

        // Trigger jump animation and play jump sound
        player.anim.SetTrigger(ANIM_JUMP);
        AudioManager.Instance.PlaySound2DOneShot(jumpSound, pitchVariation: 0.1f);
    }

    // Fast fall mechanic when airborne
    private void FastFall()
    {
        verticalVelocity = -jumpForce;
    }

    // Handle landing from jump
    private void Land()
    {
        movementState = MovementState.Running;
    }

    // Coroutine to handle sliding
    private IEnumerator Slide()
    {
        StartSliding();
        yield return new WaitForSeconds(slideDuration);
        StopSliding();
    }

    // Start sliding and modify hitbox
    private void StartSliding()
    {
        movementState = MovementState.Sliding;
        SetRegularHitbox();  // Ensure hitbox is regular before modifying
        SetSlidingHitbox();  // Set the hitbox for sliding

        // Randomize pitch for slide sound and play it
        slideSource.pitch = Random.Range(1 - slidePitchVariation, 1 + slidePitchVariation);
        slideSource.Play();
    }

    // Set the hitbox for sliding state
    private void SetSlidingHitbox()
    {
        controller.height *= slideHitboxMutliplier;
        controller.center = new Vector3(controller.center.x, controller.center.y * slideHitboxMutliplier, controller.center.z);
    }

    // Reset hitbox to its original size
    private void SetRegularHitbox()
    {
        controller.height = startingHitboxHeight;
        controller.center = startingHitboxCenter;
    }

    // Cancel sliding if interrupted
    private void CancelSlide()
    {
        if (slideRoutine != null)
            StopCoroutine(slideRoutine);
        StopSliding();
    }

    // Stop sliding and reset hitbox
    private void StopSliding()
    {
        movementState = isGrounded ? MovementState.Running : MovementState.Airborne;
        SetRegularHitbox();
        slideSource.Stop();
    }

    // Handle player movement between lanes and vertical movement
    private void Move()
    {
        // Calculate target position based on current lane
        Vector3 targetPosition = transform.position.z * Vector3.forward;
        if (desiredLane == 0) targetPosition += Vector3.left * LANE_DISTANCE;
        else if (desiredLane == 2) targetPosition += Vector3.right * LANE_DISTANCE;

        // Create a movement vector
        Vector3 moveVector = Vector3.zero;
        moveVector.x = (targetPosition - transform.position).normalized.x * speed;

        // Apply gravity and set Y-axis movement
        if (isGrounded)
            verticalVelocity = -0.1f;
        else
            verticalVelocity -= (gravity * Time.deltaTime);

        moveVector.y = verticalVelocity;
        moveVector.z = speed;

        // Move the character
        controller.Move(moveVector * Time.deltaTime);
    }

    // Rotate the player to face the direction of movement
    private void HandleRotation()
    {
        Vector3 dir = controller.velocity;
        if (dir != Vector3.zero)
        {
            dir.y = 0;
            transform.forward = Vector3.Lerp(transform.forward, dir, lookRotationDuration);
        }
    }

    // Display the trail when sliding or airborne
    private void HandleTrailDisplay()
    {
        trail.emitting = MovementState is MovementState.Airborne or MovementState.Sliding;
    }

    // Update the animations based on player movement state
    private void UpdateAnimations()
    {
        player.anim.SetBool(ANIM_RUNNING, movementState == MovementState.Running);
        player.anim.SetBool(ANIM_SLIDING, movementState == MovementState.Sliding);
        player.anim.SetBool(ANIM_GROUNDED, isGrounded);
    }

    // Move the player to the next lane based on direction
    private void MoveLane(MovementDirection direction)
    {
        desiredLane += (direction == MovementDirection.Left) ? -1 : 1;
        desiredLane = Mathf.Clamp(desiredLane, 0, 2); // Limit the lane to valid bounds
    }

    // Detect if the player is grounded using raycast
    private bool GroundedRaycast()
    {
        Ray ray = new Ray(transform.position + Vector3.up * groundedRayOffsetY, Vector3.down);
        return Physics.Raycast(ray, groundedRayTreshold, groundedLayerMask);
    }
}
