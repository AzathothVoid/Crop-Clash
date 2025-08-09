using UnityEngine;
using Photon.Pun;

public class Projectile : MonoBehaviourPun
{
    public float speed = 10f;        // How fast the projectile travels.
    public float lifetime = 5f;      // Time before self-destruction.
    public float damage = 20f;       // Damage inflicted on hit.

    private Rigidbody rb;
    private int ownerActorNumber;    // Stores the owner's actor number.

    private void Awake()
    {
        // Grab the Rigidbody and set its velocity if available.
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = transform.forward * speed;
        }
    }

    private void Start()
    {
        // Self-destruct after 'lifetime' seconds to prevent ghost projectiles.
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // If there's no Rigidbody, manually move the projectile.
        if (rb == null)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    // Set the owner of the projectile to avoid self-collisions.
    public void SetOwner(int ownerNumber)
    {
        ownerActorNumber = ownerNumber;

        // Find the owner's PhotonView and ignore collision with it.
        PhotonView ownerView = PhotonView.Find(ownerNumber);
        if (ownerView != null)
        {
            Collider projectileCollider = GetComponent<Collider>();
            Collider ownerCollider = ownerView.GetComponent<Collider>();
            if (projectileCollider != null && ownerCollider != null)
            {
                Physics.IgnoreCollision(projectileCollider, ownerCollider);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If we hit the ground, just vanish.
        if (collision.gameObject.CompareTag("Ground"))
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }

        // Check if the collided object has a PhotonView.
        PhotonView targetPV = collision.gameObject.GetComponent<PhotonView>();
        if (targetPV != null && targetPV.OwnerActorNr != ownerActorNumber)
{
    // Check if the collided object has a CapsuleCollider (assumed player character)
    CapsuleCollider capsuleCollider = collision.collider as CapsuleCollider;
    if (capsuleCollider != null)
    {
        targetPV.RPC("ApplyPunchDamage", RpcTarget.All, GetDamageValue());
    }
}

        
        // Kill the projectile on any collision.
        PhotonNetwork.Destroy(gameObject);
    }

    // Helper to get our damage value.
    private float GetDamageValue()
    {
        return damage;
    }
}
