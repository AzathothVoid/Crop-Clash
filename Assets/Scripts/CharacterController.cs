using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using ExitGames.Client.Photon;
using Cinemachine;

public class CharacterController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Player Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float maxHealth = 100f;
    public float currentHealth;
    public float mouseSensitivity = 1f;    
    public int maxLives = 2;    
    public int currentLives;
    public float rotationSpeed = 5f;

    [Header("Combat Settings")]
    public float punchRange = 20f;
    public float punchDamage = 5f;
    private float attackCooldown;

    [Header("References")]
    public Rigidbody rb;
    public GameObject characterModel;
    public Transform cameraTransform;
    public Animator animator;
    public Slider healthBar;    
    public TMP_Text livesText;
    public PunchCollider[] punchColliders;
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float projectileForce = 10f;
    public Transform cameraPivot;
    // Health Images Integration:
    public Image[] heartImages;  // Assign red heart images in Inspector.
    public Sprite blackHeartSprite;  // Assign black heart sprite in Inspector.
    public PlayerUI playerUI;

    [Header("Sounds")]
    public AudioSource runAudioSource;
    public AudioSource attackAudioSource;
    public AudioSource jumpAudioSource;
    public AudioClip runSound;
    public AudioClip attackSound;
    public AudioClip jumpSound;

    [Header("UI Panels")]
    public GameObject loserPanel; 

    [Header("Aiming Settings")]
    public float normalFOV = 60f;
    public float aimFOV = 40f;
    public float aimSpeed = 30f;
    // Projectile Aim Integration:
    [Header("Aim References")]
    public AimPositionDebugger aimDebugger;  // Assign your AimPositionDebugger in Inspector.

    // This script no longer uses its own spawnPoints.
    // The spawn points will be obtained from the GameManager.
    private Vector3 respawnPosition = new Vector3(50, 10, 40);

    private bool isGrounded;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private float turnDirection;
    private Vector3 relativeVector;
    private bool isGameOver = false;
    private readonly float minCameraAngle = -60f;
    private readonly float maxCameraAngle = 90f;
    public float minComposerOffsetY = -0.5f; 
    public float maxComposerOffsetY = 0.5f;
    public bool movement = true;
    public CinemachineVirtualCamera virtualCamera;
    private CinemachineComposer composer;

    private const byte DEATH_EVENT_CODE = 1;

    void Awake()
    {
        punchColliders = GetComponentsInChildren<PunchCollider>();
        ChangePunchColliders(false);
    }

    void Start()
    {
        if (photonView.IsMine)
        {
            currentHealth = maxHealth;
            currentLives = maxLives;
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            livesText.text = "Lives: " + currentLives;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            runAudioSource = gameObject.AddComponent<AudioSource>();
            attackAudioSource = gameObject.AddComponent<AudioSource>();
            jumpAudioSource = gameObject.AddComponent<AudioSource>();
            runAudioSource.clip = runSound;
            runAudioSource.loop = true;
            attackAudioSource.clip = attackSound;
            jumpAudioSource.clip = jumpSound;
            playerUI = GetComponent<PlayerUI>();
            composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();            

            cameraTransform.gameObject.SetActive(true);
            virtualCamera.gameObject.SetActive(true);
        }
        else
        {
            rb.isKinematic = true;
            cameraTransform.gameObject.SetActive(false);
            virtualCamera.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (photonView.IsMine && !isGameOver)
        {
            if (movement)
            {
                HandleMovement();
                HandleCamera();
            }
            CheckHealth();
            HandleAttack();
            HandleAiming();
            UpdateScore(currentLives, currentHealth);            
        }
        else
        {
            SmoothNetworkMovement();
        }
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        
        Vector3 moveDirection = (cameraTransform.forward * moveZ + cameraTransform.right * moveX).normalized;
        moveDirection.y = 0; 

        if (moveDirection.magnitude >= 0.1f)
        {
            rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.deltaTime);            

            if (!runAudioSource.isPlaying)
            {
                runAudioSource.Play();
            }            

            animator.SetBool("Run", true);
        }
        else
        {
            runAudioSource.Stop();
            animator.SetBool("Run", false);
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            jumpAudioSource.Play();
        }
    }

    void HandleCamera()
    {
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 0.1f * Time.deltaTime;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        Vector3 localEuler = cameraPivot.transform.localEulerAngles;

        float currentPitch = composer.m_TrackedObjectOffset.y;
        float newpitch = currentPitch + mouseY;

        composer.m_TrackedObjectOffset = new Vector3(0f, newpitch, 0f);

        cameraPivot.transform.parent.Rotate(Vector3.up * mouseX);
    }

    void AdjustCameraCollision()
    {
        Vector3 desiredCameraPosition = cameraPivot.position - cameraPivot.forward * 3f;
        Vector3 direction = desiredCameraPosition - cameraPivot.position;
        float sphereRadius = 0.1f;

        if (Physics.SphereCast(cameraPivot.position, sphereRadius, direction.normalized, out RaycastHit hit, direction.magnitude))
        {
            cameraTransform.position = hit.point + hit.normal * sphereRadius;
        }
        else
        {
            cameraTransform.position = desiredCameraPosition;
        }

        cameraTransform.LookAt(cameraPivot);
    }

    void HandleAiming()
    {    
        float targetFOV = Input.GetMouseButton(1) ? aimFOV : normalFOV;
        float currentFOV = virtualCamera.m_Lens.FieldOfView;
        virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * aimSpeed);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void CheckHealth()
    {
        if (currentHealth <= 0)
        {
            LoseLife();
        }
    }

    public void TakeDamage(float damage)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damage;
            healthBar.value = currentHealth;
            if (currentHealth <= 0)
            {
                LoseLife();
            }
        }
    }

    void LoseLife()
    {
        currentLives--;
        livesText.text = "Lives: " + currentLives;
        // Update health images: swap the red heart for a black heart.
        if (heartImages != null && currentLives < heartImages.Length)
        {
            heartImages[currentLives].sprite = blackHeartSprite;
        }
        if (currentLives > 0)
        {
            Respawn();
        }
        else
        {
            GameOver();
        }
    }

    // Modified Respawn() to use the spawn points defined in the GameManager script.
    void Respawn()
    {
        currentHealth = maxHealth;
        healthBar.value = currentHealth;
        
        // Obtain the GameManager instance and use its spawnPoints array.
        GameManager gm = FindObjectOfType<GameManager>(); // (Consider using FindAnyObjectByType<GameManager>() if using Unity 2023+)
        if (gm != null && gm.spawnPoints != null && gm.spawnPoints.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, gm.spawnPoints.Length);
            transform.position = gm.spawnPoints[randomIndex].position;
            transform.rotation = gm.spawnPoints[randomIndex].rotation;
        }
        else
        {
            // Fallback to the default respawn position.
            transform.position = respawnPosition;
        }
        
        rb.velocity = Vector3.zero;
    }

    void GameOver()
    {
        isGameOver = true;
        moveSpeed = 0;
        jumpForce = 0;
            
        if (rb != null)
        {            
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }
        else
        {                        
            transform.position = respawnPosition;
        }
        animator.SetBool("Run", false);
        animator.SetTrigger("Die");
        livesText.text = "Lives: 0 - Game Over";
        
        if (photonView.IsMine)
        {
            playerUI.ShowLocalLoserUI();
            //loserPanel.SetActive(true);            

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "IsDead", true }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            StartCoroutine(DelayedDestroy());
        }
    }

    IEnumerator DelayedDestroy()
    {
        yield return new WaitForSecondsRealtime(10f);
        PhotonNetwork.Destroy(gameObject);

        if (photonView.IsMine)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    void HandleAttack()
    {    
        if (Input.GetMouseButtonDown(0) && Time.time > attackCooldown)
        {
            animator.SetTrigger("Punch");
            attackCooldown = Time.time + 1.4f;
            ChangePunchColliders(true);
            ResetPunchColliders();

            StartCoroutine(DisablePunchColliderAfterDelay());
        }
     
        if (Input.GetKeyDown(KeyCode.E))
        {
            animator.SetTrigger("Throw");
            // Use the projectile aim functionality via the aimDebugger.
            photonView.RPC("ThrowProjectile", RpcTarget.All);
            if (attackAudioSource != null)
            {
                attackAudioSource.Play();
            }
        }
    }
    
    private IEnumerator DisablePunchColliderAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        ChangePunchColliders(false);
    }

    private void ChangePunchColliders(bool val)
    {
        foreach (PunchCollider collider in punchColliders)
        {
            if (collider != null)
            {
                collider.gameObject.SetActive(val);
            }
        }
    }

    private void ResetPunchColliders()
    {
        foreach (PunchCollider collider in punchColliders)
        {
            if (collider != null)
            {
                StartCoroutine(collider.ResetHit());
            }
        }
    }

    public void SetToMaxHealth()
    {
        currentHealth = maxHealth;
        healthBar.value = currentHealth;
    }

    [PunRPC]
void ThrowProjectile()
{
    // Create a ray from the camera's position through the center of the screen
    Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
    RaycastHit hit;

    // Determine the direction to throw the projectile
    Vector3 shootDirection;
    if (Physics.Raycast(ray, out hit, Mathf.Infinity))
    {
        // If the ray hits something, throw the projectile towards the hit point
        shootDirection = (hit.point - projectileSpawnPoint.position).normalized;
    }
    else
    {
        // If the ray doesn't hit anything, throw the projectile in the camera's forward direction
        shootDirection = cameraTransform.forward;
    }

    // Spawn and throw the projectile
    GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(shootDirection));
    Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
    if (projectileRb != null)
    {
        projectileRb.AddForce(shootDirection * projectileForce, ForceMode.Impulse);
    }
}

    [PunRPC]
    public void ApplyPunchDamage(float damage)
    {
        TakeDamage(damage);
    }

    void SmoothNetworkMovement()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(currentHealth);
            stream.SendNext(currentLives);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            currentHealth = (float)stream.ReceiveNext();
            currentLives = (int)stream.ReceiveNext();
            livesText.text = "Lives: " + currentLives;
        }
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

    public void UpdateScore(int lives, float health)
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "Lives", lives },
            { "Health", health }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
}