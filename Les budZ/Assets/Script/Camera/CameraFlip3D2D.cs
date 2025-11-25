using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFlip3D2D : MonoBehaviour
{
    [Header("Suivi de la cible")]
    [Tooltip("Par défaut, la caméra va chercher GameManager.instance.targetTransform")]
    public Transform target;
    public float followSpeed = 5f;

    [Header("Offsets 3D")]
    [Tooltip("Position relative à la target en mode 3D (monde)")]
    public Vector3 positionOffset3D = new Vector3(0f, 0f, -10f);
    [Tooltip("Rotation de la caméra en mode 3D (Euler)")]
    public Vector3 rotationEuler3D = new Vector3(10f, 0f, 0f);

    [Header("Offsets 2D")]
    [Tooltip("Position relative à la target en mode 2D (monde)")]
    public Vector3 positionOffset2D = new Vector3(0f, 8f, -10f);
    [Tooltip("Rotation de la caméra en mode 2D (Euler)")]
    public Vector3 rotationEuler2D = new Vector3(0f, 0f, 0f);

    [Header("Flip 3D <-> 2D")]
    [Tooltip("Vrai = caméra en mode 3D (Perspective)")]
    public bool is3D = true;

    [Min(0.01f)]
    public float flipDuration = 1f;

    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("FOV 'zoomé' utilisé pendant les flips (valeur minimale)")]
    [Range(5f, 89f)]
    public float minFovDuringFlip = 10f;

    [Header("Mapping FOV <-> Size")]
    [Tooltip("Size = baseFov / mappingRatio (utilisé pour la size finale en ortho)")]
    public float mappingRatio = 10f;

    [SerializeField, Tooltip("FOV de référence (mesuré une fois au start)")]
    private float baseFov;

    [SerializeField, Tooltip("Size cible = baseFov / mappingRatio")]
    private float baseSize;

    private bool mappingInitialized = false;
    private Camera cam;
    private bool isFlipping = false;

    // ================== INIT ==================

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("[CameraFlip3D2D] Aucun composant Camera trouvé.");
            enabled = false;
            return;
        }

        is3D = !cam.orthographic;
        InitMapping();
    }

    /// <summary>
    /// On fixe UNE FOIS baseFov et baseSize à partir de l'état au démarrage.
    /// </summary>
    private void InitMapping()
    {
        if (mappingInitialized) return;

        if (!cam.orthographic)
        {
            // On démarre en 3D : FOV de départ = référence
            baseFov = cam.fieldOfView;
            baseSize = baseFov / mappingRatio;
        }
        else
        {
            // On démarre en 2D : size de départ → FOV équivalent
            baseSize = cam.orthographicSize;
            baseFov = baseSize * mappingRatio;
        }

        mappingInitialized = true;
    }

    void LateUpdate()
    {
        UpdateTargetReference();

        if (!isFlipping && target != null)
        {
            Vector3 desiredPos = GetTargetPosition(is3D);
            Quaternion desiredRot = GetTargetRotation(is3D);

            transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, followSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetTargetPosition(bool threeD)
    {
        if (target == null) return transform.position;
        return target.position + (threeD ? positionOffset3D : positionOffset2D);
    }

    private Quaternion GetTargetRotation(bool threeD)
    {
        return Quaternion.Euler(threeD ? rotationEuler3D : rotationEuler2D);
    }

    /// <summary>
    /// Essaie de récupérer la target via GameManager.instance si besoin.
    /// </summary>
    private void UpdateTargetReference()
    {
        if (target == null)
        {
            if (GameManager.instance != null)
            {
                target = GameManager.instance.targetTransform;
            }

            if (target == null)
            {
                Debug.Log("camera cannot find target !!!");
            }
        }
    }

    // ================== APPELS PUBLICS ==================

    public void Flip3Dto2D()
    {
        if (!is3D) return;
        if (isFlipping) return;

        StartCoroutine(Flip3Dto2DRoutine());
    }

    public void Flip2Dto3D()
    {
        if (is3D) return;
        if (isFlipping) return;

        StartCoroutine(Flip2Dto3DRoutine());
    }

    // ================== 3D -> 2D (on garde ta version qui marche) ==================

    private IEnumerator Flip3Dto2DRoutine()
    {
        isFlipping = true;
        InitMapping();
        UpdateTargetReference();

        Transform currentTarget = target;

        // État de départ (3D)
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float startFov = cam.fieldOfView; // devrait être = baseFov

        // Offsets de départ / d'arrivée
        Vector3 startOffset = (currentTarget != null) ? (startPos - currentTarget.position) : startPos;
        Vector3 endOffset = positionOffset2D;

        Quaternion endRot = Quaternion.Euler(rotationEuler2D);

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            float t = elapsed / flipDuration;
            float eased = flipCurve.Evaluate(t);

            // Rotation interpolée
            Quaternion currRot = Quaternion.Slerp(startRot, endRot, eased);
            Vector3 forward = currRot * Vector3.forward;

            // Offset interpolé
            Vector3 baseOffset = Vector3.Lerp(startOffset, endOffset, eased);
            Vector3 lateral = baseOffset - Vector3.Project(baseOffset, -forward);

            // FOV descend : baseFov -> minFovDuringFlip
            float currFov = Mathf.Lerp(startFov, minFovDuringFlip, eased);

            // Dolly pour garder une hauteur approx. constante (baseSize)
            float halfRad = currFov * Mathf.Deg2Rad * 0.5f;
            float dist = baseSize / Mathf.Tan(halfRad);

            if (currentTarget != null)
                currentTarget = target;

            Vector3 targetPos = currentTarget != null ? currentTarget.position : Vector3.zero;
            Vector3 currPos = targetPos + lateral - forward * dist;

            transform.position = currPos;
            transform.rotation = currRot;

            cam.orthographic = false;
            cam.fieldOfView = currFov;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap final en 2D, mapping respecté : size = baseFov / mappingRatio
        Vector3 finalPos;
        if (currentTarget != null)
            finalPos = currentTarget.position + positionOffset2D;
        else
            finalPos = positionOffset2D;

        transform.position = finalPos;
        transform.rotation = Quaternion.Euler(rotationEuler2D);

        cam.orthographic = true;
        cam.orthographicSize = baseSize; // = baseFov / mappingRatio
        cam.fieldOfView = baseFov;       // ref pour le retour

        is3D = false;
        isFlipping = false;
        if (GameManager.instance != null)
            GameManager.instance.ChangeDimensionState(is3D);
    }

    // ================== 2D -> 3D (NOUVEAU TRAVELLING COMPENSÉ) ==================

    private IEnumerator Flip2Dto3DRoutine()
    {
        isFlipping = true;
        InitMapping();
        UpdateTargetReference();

        Transform currentTarget = target;

        // 0) On force un état 2D "propre" avant de commencer
        Quaternion rot2D = Quaternion.Euler(rotationEuler2D);
        Quaternion rot3D = Quaternion.Euler(rotationEuler3D);

        if (currentTarget != null)
        {
            transform.position = currentTarget.position + positionOffset2D;
        }
        transform.rotation = rot2D;
        cam.orthographic = true;
        cam.orthographicSize = baseSize;

        // 1) On bascule DIRECT en perspective avec FOV minimal
        cam.orthographic = false;
        float startFov = minFovDuringFlip;
        cam.fieldOfView = startFov;

        // On calcule la position de départ en perspective pour garder la même échelle (baseSize)
        Vector3 forward0 = rot2D * Vector3.forward;

        // Offset de base au début : offset 2D
        Vector3 baseOffset2D = positionOffset2D;
        Vector3 lateral2D = baseOffset2D - Vector3.Project(baseOffset2D, -forward0);

        float halfRad0 = startFov * Mathf.Deg2Rad * 0.5f;
        float dist0 = baseSize / Mathf.Tan(halfRad0);

        Vector3 startPosPersp;
        if (currentTarget != null)
        {
            startPosPersp = currentTarget.position + lateral2D - forward0 * dist0;
        }
        else
        {
            startPosPersp = lateral2D - forward0 * dist0;
        }

        // 2) State de départ du flip en 2D->3D (en perspective déjà)
        Vector3 startPos = startPosPersp;
        Quaternion startRot = rot2D;

        // Offsets de référence (pour interpolation)
        Vector3 baseOffset3D = positionOffset3D;

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            float t = elapsed / flipDuration;
            float eased = flipCurve.Evaluate(t);

            // Rotation interpolée 2D -> 3D
            Quaternion currRot = Quaternion.Slerp(startRot, rot3D, eased);
            Vector3 forward = currRot * Vector3.forward;

            // Offset interpolé entre 2D et 3D (en monde)
            Vector3 baseOffset = Vector3.Lerp(baseOffset2D, baseOffset3D, eased);

            // On garde seulement la partie latérale, la distance sera gérée par le dolly
            Vector3 lateral = baseOffset - Vector3.Project(baseOffset, -forward);

            // FOV qui augmente : minFovDuringFlip -> baseFov
            float currFov = Mathf.Lerp(startFov, baseFov, eased);
            float halfRad = currFov * Mathf.Deg2Rad * 0.5f;
            float dist = baseSize / Mathf.Tan(halfRad);

            Vector3 targetPos = currentTarget != null ? currentTarget.position : Vector3.zero;
            Vector3 currPos = targetPos + lateral - forward * dist;

            transform.position = currPos;
            transform.rotation = currRot;
            cam.fieldOfView = currFov;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3) Snap final en 3D cohérent avec ton offset 3D
        Vector3 endPos;
        if (currentTarget != null)
            endPos = currentTarget.position + positionOffset3D;
        else
            endPos = positionOffset3D;

        transform.position = endPos;
        transform.rotation = rot3D;
        cam.orthographic = false;
        cam.fieldOfView = baseFov;

        is3D = true;
        isFlipping = false;
        if (GameManager.instance != null)
            GameManager.instance.ChangeDimensionState(is3D);
    }
}
