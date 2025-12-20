using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMovement3D playerMovement3D;
    [SerializeField] private TimeWindowActivator timeWindowActivator;

    private bool Online => GameManager.instance != null && GameManager.instance.isGameOnline;

    private void Awake()
    {
        CacheRefs();
        if (Online)
            SetEnabledState(false);
        else
            SetEnabledState(true);
    }

    private void Start()
    {
        if (!Online)
            SetEnabledState(true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Online)
            SetEnabledState(IsOwner);
        else
            SetEnabledState(true);
    }

    private void CacheRefs()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (playerMovement3D == null) playerMovement3D = GetComponent<PlayerMovement3D>();
        if (timeWindowActivator == null) timeWindowActivator = GetComponent<TimeWindowActivator>();
    }

    private void SetEnabledState(bool value)
    {
        if (playerInput != null) playerInput.enabled = value;
        if (playerMovement3D != null) playerMovement3D.enabled = value;
        if (timeWindowActivator != null) timeWindowActivator.enabled = value;
    }
}