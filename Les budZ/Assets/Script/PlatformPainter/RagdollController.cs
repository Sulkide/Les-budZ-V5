using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [Header("Racine du perso (mouvement normal)")]
    [SerializeField] private PlayerMovement3D  pm;
    [SerializeField] private Rigidbody rootRigidbody;
    [SerializeField] private List<Collider> rootColliders = new();

    [Header("Animator qui pilote le rig en mode normal")]
    [SerializeField] private Animator animator;

    [Header("Bones de ragdoll (calculés auto si vide)")]
    [SerializeField] private Rigidbody[] ragdollRigidbodies;
    [SerializeField] private Collider[] ragdollColliders;
    
    [SerializeField] private Rigidbody mainBone;
    
    private float deathUpImpulse = 100f;
    
    private float deathTorqueZ = 100f;

    public bool IsRagdollActive { get; private set; }

    private void Awake()
    {
        if (pm == null)
            pm = GetComponent<PlayerMovement3D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (rootRigidbody == null && pm != null)
            rootRigidbody = pm.rb;

        // Colliders de gameplay à désactiver en ragdoll
        rootColliders.Clear();
        if (pm != null)
        {
            rootColliders.Add(pm.colliderBottomBody);
            rootColliders.Add(pm.collideTopBody);
            rootColliders.Add(pm.colliderFeet);
            rootColliders.Add(pm.colliderDash);
            rootColliders.Add(pm.colliderGroundPound);
            rootColliders.Add(pm.colliderWeapon);
        }

        // === AUTO-FILL SÉCURISÉ ===

        // 1) Tous les rigidbodies enfants, sauf la racine
        if (ragdollRigidbodies == null || ragdollRigidbodies.Length == 0)
        {
            var rbs = GetComponentsInChildren<Rigidbody>(true).ToList();
            if (rootRigidbody != null)
                rbs.Remove(rootRigidbody);
            ragdollRigidbodies = rbs.ToArray();
        }

        // 2) Tous les colliders enfants, sauf les colliders de gameplay
        if (ragdollColliders == null || ragdollColliders.Length == 0)
        {
            var cols = GetComponentsInChildren<Collider>(true).ToList();
            foreach (var c in rootColliders)
            {
                if (c != null)
                    cols.Remove(c);
            }
            ragdollColliders = cols.ToArray();
        }

        EnableRagdoll(false);
    }

    public void EnableRagdoll(bool state)
    {
        IsRagdollActive = state;
        
        if (animator != null)
            animator.enabled = !state;
        
        if (rootRigidbody != null)
        {
            if (state)
            {
                rootRigidbody.linearVelocity  = Vector3.zero;
                rootRigidbody.angularVelocity = Vector3.zero;
                rootRigidbody.isKinematic     = true;
            }
            else
            {
                rootRigidbody.isKinematic = false;
            }
        }
        
        foreach (var c in rootColliders)
        {
            if (c == null) continue;
            c.enabled = !state;
        }
        
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;

            if (state)
            {
                rb.isKinematic = false;
                rb.useGravity  = true;

                rb.collisionDetectionMode   = CollisionDetectionMode.ContinuousDynamic;
                rb.maxDepenetrationVelocity = 8f;     
                rb.interpolation            = RigidbodyInterpolation.Interpolate;
                
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                rb.isKinematic = true;
                rb.useGravity  = false;

                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    
        foreach (var col in ragdollColliders)
        {
            if (col == null) continue;
            if (col.isTrigger) continue;

            col.enabled = state;
        }
    }


    public void PlayDeathImpulse(bool facingRight)
    {
        if (!IsRagdollActive) return;

        Rigidbody target = mainBone;

        if (target == null)
        {
            if (ragdollRigidbodies != null && ragdollRigidbodies.Length > 0)
                target = ragdollRigidbodies[0];
            else
                return;
        }


        if (deathUpImpulse != 0f)
        {
            float sign = facingRight ? 1f : -1f;
            foreach (var rb in ragdollRigidbodies)
            {
                rb.AddForce(new Vector3(sign, 1,0f)*deathUpImpulse, ForceMode.Impulse);
            }
        }
        
        if (deathTorqueZ != 0f)
        {
            float sign = facingRight ? 1f : -1f;
            target.AddTorque(new Vector3(0f, 0f, deathTorqueZ * sign), ForceMode.Force);
        }
    }

    
}
