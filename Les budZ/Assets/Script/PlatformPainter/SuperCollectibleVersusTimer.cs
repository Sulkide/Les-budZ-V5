using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SuperCollectibleVersusTimer : NetworkBehaviour
{
    [Header("Mouvement / Physique")]
    [SerializeField] private float initialImpulse = 6f;
    [SerializeField] private float randomHorizontalImpulse = 3f;
    [SerializeField] private float randomTorque = 10f;

    [Header("Respawn en version statique")]
    [Tooltip("Prefab du SuperCollectibleVersus de base (forme initiale).")]
    [SerializeField] private GameObject superCollectiblePrefab;

    private int scorePerTick;
    private float intervalSeconds;
    private float lifetime;
    private float pickupLockDuration;

    private int previousOwnerId = -1;
    private float spawnTime;
    private bool initialized;

    private Rigidbody rb;
    private Collider col;
    
    public void Initialize(
        PlayerMovement3D previousOwner,
        int scorePerTick,
        float intervalSeconds,
        float lifetime,
        float pickupLockDuration)
    {
        if (!IsServer) return;

        this.scorePerTick      = scorePerTick;
        this.intervalSeconds   = intervalSeconds;
        this.lifetime          = lifetime;
        this.pickupLockDuration = pickupLockDuration;

        this.previousOwnerId   = (previousOwner != null) ? previousOwner.playerID : -1;
        this.spawnTime         = Time.time;
        this.initialized       = true;

        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        SetupPhysics();

        StartCoroutine(LifeRoutine());
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (IsServer && !initialized)
        {
            spawnTime = Time.time;
            StartCoroutine(LifeRoutine());
        }
    }
    
    private void SetupPhysics()
    {
        if (rb == null) return;
        
        Vector3 dir = new Vector3(
            Random.Range(-1f, 1f),
            1f,
            Random.Range(-1f, 1f)
        ).normalized;

        Vector3 impulse = dir * initialImpulse;
        
        impulse.x += Random.Range(-randomHorizontalImpulse, randomHorizontalImpulse);
        impulse.z += Random.Range(-randomHorizontalImpulse, randomHorizontalImpulse);

        rb.AddForce(impulse, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
    }

    private IEnumerator LifeRoutine()
    {
        float endTime = spawnTime + lifetime;

        while (Time.time < endTime)
        {
            yield return null;
        }

        if (!IsSpawned) yield break;

        ReturnToStaticCollectible();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerMovement3D player = other.GetComponentInParent<PlayerMovement3D>();
        if (player == null) return;
        
        if (player.playerID == previousOwnerId &&
            Time.time < spawnTime + pickupLockDuration)
        {
            Debug.Log($"[SuperBonusTimer] Player {player.playerID} est l'ancien owner, lock encore actif.");
            return;
        }

        Debug.Log($"[SuperBonusTimer] Player {player.playerID} récupère le Timer, scorePerTick={scorePerTick}, interval={intervalSeconds}");
        
        player.GainSuperCollectible(scorePerTick, intervalSeconds);
        
        DespawnSelf();
    }

    private void ReturnToStaticCollectible()
    {
        if (superCollectiblePrefab == null)
        {
            Debug.LogWarning("[SuperBonusTimer] superCollectiblePrefab non assigné, on despawn simplement.");
            DespawnSelf();
            return;
        }

        Vector3 spawnPos = transform.position;
        
        GameObject levelObj = GameObject.FindGameObjectWithTag("Level");
        if (levelObj != null)
        {
            RespawnPointList list = levelObj.GetComponent<RespawnPointList>();
            if (list != null && list.respawnPoint != null && list.respawnPoint.Count > 0)
            {
                int index = Random.Range(0, list.respawnPoint.Count);
                if (list.respawnPoint[index] != null)
                {
                    spawnPos = list.respawnPoint[index].position;
                }
            }
        }
        
        GameObject newObj = Instantiate(superCollectiblePrefab, new Vector3(spawnPos.x, spawnPos.y+4, spawnPos.z), Quaternion.identity);
        NetworkObject newNetObj = newObj.GetComponent<NetworkObject>();
        if (newNetObj != null && !newNetObj.IsSpawned)
        {
            newNetObj.Spawn(true);
        }

        DespawnSelf();
    }

    private void DespawnSelf()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
