using UnityEngine;
using System.Collections;
using Photon.Pun;

public class PunchCollider : MonoBehaviourPun
{
    public float punchDamage = 15f;
    private bool hasHit = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        Debug.Log("Inside Trigger");

        if (other.CompareTag("Player") && other.gameObject != transform.root.gameObject)
        {
            PhotonView targetPV = other.GetComponentInParent<PhotonView>();

            Debug.Log("Inside Core Area");
            if (targetPV != null)
            {
                Debug.Log($"{photonView.Owner.NickName} punched {targetPV.Owner.NickName}");

                targetPV.RPC("ApplyPunchDamage", RpcTarget.All, punchDamage);
                hasHit = true;
            }
        }
    }

    public IEnumerator ResetHit()
    {
        yield return new WaitForSeconds(1);
        hasHit = false;
    }
}
