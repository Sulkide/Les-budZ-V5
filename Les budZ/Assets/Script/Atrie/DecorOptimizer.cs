using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DecorOptimizer : MonoBehaviour
{
    [Header("Caméra utilisée (in-game)")]
    [Tooltip("Si vide, utilise Camera.main au runtime.")]
    public Camera targetCamera;

    [Header("Ciblage des enfants")]
    [Tooltip("Si vide, tous les enfants directs de ce parent seront togglés.")]
    public List<Transform> targetsToToggle = new List<Transform>();

    [Header("Réglages")]
    [Tooltip("Temps entre deux tests de visibilité (plus grand = moins de CPU).")]
    [Min(0.01f)] public float checkInterval = 0.1f;

    [Tooltip("Marge ajoutée à la bbox (en % de sa taille) pour éviter le popping.")]
    [Range(0f, 1f)] public float boundsPadding = 0.15f;

    [Header("Pré-affichage (mètres)")]
    [Tooltip("Active les éléments quand ils sont à moins de X mètres du bord de l'écran.")]
    [Min(0f)] public float prewarmDistance = 5f;

    
    [Tooltip("Activer les enfants au Start avant la première vérification.")]
    public bool forceActiveOnStart = true;
    
    private Bounds _localGroupBounds;
    private float _nextCheckTime;
    private bool _lastVisible = true;
    private bool _hasBounds;

    void Awake()
    {
        if (targetsToToggle == null || targetsToToggle.Count == 0)
        {
            targetsToToggle = new List<Transform>(transform.childCount);
            for (int i = 0; i < transform.childCount; i++)
                targetsToToggle.Add(transform.GetChild(i));
        }
        
        CacheLocalBounds();

        if (forceActiveOnStart)
            ApplyVisibility(true);
    }

    void Start()
    {
       
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return; 
        }

        bool visible = IsGroupVisible(targetCamera);
        if (visible != _lastVisible)
        {
            ApplyVisibility(visible);
            _lastVisible = visible;
        }
    }

    private void ApplyVisibility(bool on)
    {
        for (int i = 0; i < targetsToToggle.Count; i++)
        {
            var t = targetsToToggle[i];
            if (!t) continue;
            if (t.gameObject.activeSelf != on)
                t.gameObject.SetActive(on);
        }
    }

    private void CacheLocalBounds()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            _localGroupBounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);
            _hasBounds = true;
            return;
        }

       
        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            world.Encapsulate(renderers[i].bounds);

        _localGroupBounds = WorldBoundsToLocal(world, transform.worldToLocalMatrix);

        if (boundsPadding > 0f)
        {
            Vector3 extra = _localGroupBounds.size * boundsPadding;
            _localGroupBounds.Expand(extra);
        }

        _hasBounds = true;
    }

    private bool IsGroupVisible(Camera cam)
    {
        if (!_hasBounds) return true;

        Bounds worldBounds = TransformBounds(transform.localToWorldMatrix, _localGroupBounds);

        if (prewarmDistance > 0f)
        {
            worldBounds.Expand(new Vector3(prewarmDistance * 2f, prewarmDistance * 2f, prewarmDistance * 2f));
        }

        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, worldBounds);
    }

    private static Bounds WorldBoundsToLocal(Bounds world, Matrix4x4 worldToLocal)
    {
        Vector3 min = world.min;
        Vector3 max = world.max;

        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(min.x, max.y, min.z);
        corners[3] = new Vector3(max.x, max.y, min.z);
        corners[4] = new Vector3(min.x, min.y, max.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(min.x, max.y, max.z);
        corners[7] = new Vector3(max.x, max.y, max.z);

        Bounds local = new Bounds(worldToLocal.MultiplyPoint3x4(corners[0]), Vector3.zero);
        for (int i = 1; i < 8; i++)
            local.Encapsulate(worldToLocal.MultiplyPoint3x4(corners[i]));
        return local;
    }

    private static Bounds TransformBounds(Matrix4x4 localToWorld, Bounds local)
    {
        Vector3 min = local.min;
        Vector3 max = local.max;

        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(min.x, max.y, min.z);
        corners[3] = new Vector3(max.x, max.y, min.z);
        corners[4] = new Vector3(min.x, min.y, max.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(min.x, max.y, max.z);
        corners[7] = new Vector3(max.x, max.y, max.z);

        Bounds world = new Bounds(localToWorld.MultiplyPoint3x4(corners[0]), Vector3.zero);
        for (int i = 1; i < 8; i++)
            world.Encapsulate(localToWorld.MultiplyPoint3x4(corners[i]));
        return world;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!_hasBounds) return;

        var world = TransformBounds(transform.localToWorldMatrix, _localGroupBounds);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        Gizmos.DrawCube(world.center, world.size);
        Gizmos.color = new Color(0f, 1f, 0.4f, 1f);
        Gizmos.DrawWireCube(world.center, world.size);
    }
#endif
}
