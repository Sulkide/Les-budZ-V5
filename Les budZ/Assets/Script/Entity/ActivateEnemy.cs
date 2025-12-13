using System.Collections.Generic;
using UnityEngine;

public class ActivationScripts : MonoBehaviour
{
    public Collider2D triggerCollider;
    
    public List<MonoBehaviour> scriptsToActivate;
    
    private Collider2D[] results = new Collider2D[10];

    private int layerPlayer;
    private int layerProjectile;
    private int layerProjectileCollision;

    private void Awake()
    {
        layerPlayer = LayerMask.NameToLayer("Player");
        layerProjectile = LayerMask.NameToLayer("Projectile");
        layerProjectileCollision = LayerMask.NameToLayer("ProjectileCollision");
    }

    private void Update()
    {
        if (triggerCollider == null)
        {
            Debug.LogError("Le collider de détection n'est pas assigné dans l'inspecteur !");
            return;
        }
        
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = false; 
        
        int count = triggerCollider.Overlap(filter, results);
        
        for (int i = 0; i < count; i++)
        {
            int objLayer = results[i].gameObject.layer;
            if (objLayer == layerPlayer || objLayer == layerProjectile || objLayer == layerProjectileCollision)
            {
                foreach (MonoBehaviour script in scriptsToActivate)
                {
                    if (script != null)
                    {
                        script.enabled = true;
                    }
                }
                triggerCollider.enabled = false;
                return; 
            }
        }
    }
}
