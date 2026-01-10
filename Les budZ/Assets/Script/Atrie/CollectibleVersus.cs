using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider))]
public class CollectibleVersus : NetworkBehaviour
{
    [Header("Score")]
    [SerializeField] private int scoreValue = 100;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 30f;
    
    private Collider triggerCollider;
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
        
        player.AddScore(scoreValue);

        Collect();
    }

    private void Collect()
    {
        isAvailable = false;
        SetActiveStateClientRpc(false);

        if (respawnDelay > 0f)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        isAvailable = true;
        SetActiveStateClientRpc(true);
    }

    [ClientRpc]
    private void SetActiveStateClientRpc(bool visible)
    {
        if (triggerCollider != null)
            triggerCollider.enabled = visible;

        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }
    }
    
    public void SetRespawnDelay(float value)
    {
        respawnDelay = Mathf.Max(0f, value);
    }
}
