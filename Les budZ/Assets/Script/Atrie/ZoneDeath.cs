using UnityEngine;
using Unity.Netcode;  

[RequireComponent(typeof(Collider))]
public class ZoneDeath : MonoBehaviour
{
    [Header("Paramètres de la zone de mort")]
    [Tooltip("Dégâts envoyés au joueur. Si <= 0, on utilisera la vie courante du joueur pour être sûr de le tuer.")]
    public int killDamage = 20;

    [Tooltip("Force de knockback appliquée au joueur (optionnel).")]
    public float knockbackForce = 0f;

    [Tooltip("Durée de stun après le hit (pas utile ici, laisser 0 si tu veux juste tuer).")]
    public float stunDuration = 0f;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
      
        PlayerMovement3D player = other.GetComponentInParent<PlayerMovement3D>();
        if (player == null)
            return;
        
        
        Vector3 dir = (player.transform.position - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector3.up;

        int damageToApply = killDamage > 0 ? killDamage : player.currentLife.Value;
        
        player.GetHit(dir, knockbackForce, damageToApply, stunDuration, player.isFacingRight);
    }
}