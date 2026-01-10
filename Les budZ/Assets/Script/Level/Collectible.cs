using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class Collectible : NetworkBehaviour
{
    [FormerlySerializedAs("currentLife")]
    [Header("Score options")]
    [SerializeField] private int amount = 100;

    [SerializeField] private bool canRespawn = true;
    [SerializeField] private float lifeTime = 6f;

    [Header("Visual / Collisions")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Collider[] colliders;

    // Online : état partagé (server -> clients)
    private readonly NetworkVariable<bool> isAvailableNet =
        new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    // Offline fallback (comme ton script actuel)
    private bool _isAvailableOffline = true;

    private Coroutine respawnRoutineServer;

    private bool Online => GameManager.instance != null && GameManager.instance.isGameOnline; // cohérent avec ton projet :contentReference[oaicite:2]{index=2}

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>(true);
    }

    public override void OnNetworkSpawn()
    {
        isAvailableNet.OnValueChanged += OnAvailableChanged;
        ApplyAvailability(isAvailableNet.Value);
    }

    public override void OnNetworkDespawn()
    {
        isAvailableNet.OnValueChanged -= OnAvailableChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        // OFFLINE
        if (!Online)
        {
            if (!_isAvailableOffline) return;

            var pm = other.GetComponentInParent<PlayerMovement3D>();
            if (pm == null) return;

            pm.AddScore(amount);

            if (canRespawn) StartCoroutine(RespawnOffline(lifeTime));
            else Destroy(gameObject);

            return;
        }

        // ONLINE
        if (!isAvailableNet.Value) return;

        if (IsServer)
        {
            var pm = other.GetComponentInParent<PlayerMovement3D>();
            TryPickupServer(pm);
            return;
        }

        var playerNO = other.GetComponentInParent<NetworkObject>();
        if (playerNO != null)
            RequestPickupServerRpc(playerNO.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(ulong playerNetworkObjectId)
    {
        if (!isAvailableNet.Value) return;

        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out var playerObj))
            return;

        var pm = playerObj.GetComponentInChildren<PlayerMovement3D>();
        if (pm == null) pm = playerObj.GetComponent<PlayerMovement3D>();
        if (pm == null) return;


        if (Vector3.Distance(pm.transform.position, transform.position) > 3f)
            return;

        TryPickupServer(pm);
    }

    private void TryPickupServer(PlayerMovement3D pm)
    {
        if (!IsServer) return;
        if (!isAvailableNet.Value) return;
        if (pm == null) return;
        
        pm.AddScore(amount);

        if (canRespawn)
        {
            isAvailableNet.Value = false;

            if (respawnRoutineServer != null)
                StopCoroutine(respawnRoutineServer);

            respawnRoutineServer = StartCoroutine(ServerRespawnRoutine(Mathf.Max(0.01f, lifeTime)));
        }
        else
        {
            var netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(destroy: true);
            else
                Destroy(gameObject);
        }
    }

    private IEnumerator ServerRespawnRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        isAvailableNet.Value = true;
        respawnRoutineServer = null;
    }

    private IEnumerator RespawnOffline(float time)
    {
        ApplyAvailability(false);
        _isAvailableOffline = false;

        yield return new WaitForSeconds(time);

        ApplyAvailability(true);
        _isAvailableOffline = true;
    }

    private void OnAvailableChanged(bool previousValue, bool newValue)
    {
        ApplyAvailability(newValue);
    }

    private void ApplyAvailability(bool available)
    {
        if (renderers != null)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = available;
        }
        
        if (colliders != null)
        {
            foreach (var c in colliders)
                if (c != null) c.enabled = available;
        }
    }
}
