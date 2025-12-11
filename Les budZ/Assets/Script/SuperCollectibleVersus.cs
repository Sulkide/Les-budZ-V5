using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider))]
public class SuperCollectibleVersus : NetworkBehaviour
{
    [Header("Super Collectible Versus")]
    [SerializeField] private int scorePerTick = 10;  
    [SerializeField] private float tickInterval = 2f; 

    [Header("Visual & Trigger")]
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private Renderer[] renderers;

    private bool isAvailable = true;

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!isAvailable) return;

        PlayerMovement3D player = other.GetComponentInParent<PlayerMovement3D>();
        if (player == null) return;
        if (player.isDead.Value) return;
        player.GainSuperCollectible(scorePerTick, tickInterval);

        Collect();
    }

    private void Collect()
    {
        isAvailable = false;

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                    r.enabled = false;
            }
        }
        
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}