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
    public Camera cam;
    private bool isFlipping = false;



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
        
        GameManager.instance.RegisterCameraOption(cam, this);
    }

    private void InitMapping()
    {
        if (mappingInitialized) return;

        if (!cam.orthographic)
        {
            baseFov = cam.fieldOfView;
            baseSize = baseFov / mappingRatio;
        }
        else
        {
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

    private IEnumerator Flip3Dto2DRoutine()
    {
        isFlipping = true;
        InitMapping();
        UpdateTargetReference();
        
        is3D = false;
        if (GameManager.instance != null)
            GameManager.instance.ChangeDimensionState(is3D);

        Transform currentTarget = target;
        
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float startFov = cam.fieldOfView; 
        
        Vector3 startOffset = (currentTarget != null) ? (startPos - currentTarget.position) : startPos;
        Vector3 endOffset = positionOffset2D;

        Quaternion endRot = Quaternion.Euler(rotationEuler2D);

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            float t = elapsed / flipDuration;
            float eased = flipCurve.Evaluate(t);
            Quaternion currRot = Quaternion.Slerp(startRot, endRot, eased);
            Vector3 forward = currRot * Vector3.forward;
            
            Vector3 baseOffset = Vector3.Lerp(startOffset, endOffset, eased);
            Vector3 lateral = baseOffset - Vector3.Project(baseOffset, -forward);

            float currFov = Mathf.Lerp(startFov, minFovDuringFlip, eased);
            
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
        
        Vector3 finalPos;
        if (currentTarget != null)
            finalPos = currentTarget.position + positionOffset2D;
        else
            finalPos = positionOffset2D;

        transform.position = finalPos;
        transform.rotation = Quaternion.Euler(rotationEuler2D);

        cam.orthographic = true;
        cam.orthographicSize = baseSize; 
        cam.fieldOfView = baseFov;     


        isFlipping = false;

    }

    
    private IEnumerator Flip2Dto3DRoutine()
    {
        isFlipping = true;
        InitMapping();
        UpdateTargetReference();

        is3D = true;
        if (GameManager.instance != null)
            GameManager.instance.ChangeDimensionState(is3D);
        
        Transform currentTarget = target;
        
        Quaternion rot2D = Quaternion.Euler(rotationEuler2D);
        Quaternion rot3D = Quaternion.Euler(rotationEuler3D);

        if (currentTarget != null)
        {
            transform.position = currentTarget.position + positionOffset2D;
        }
        transform.rotation = rot2D;
        cam.orthographic = true;
        cam.orthographicSize = baseSize;
        
        cam.orthographic = false;
        float startFov = minFovDuringFlip;
        cam.fieldOfView = startFov;

      
        Vector3 forward0 = rot2D * Vector3.forward;
        
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
        
        Vector3 startPos = startPosPersp;
        Quaternion startRot = rot2D;
        
        Vector3 baseOffset3D = positionOffset3D;

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            float t = elapsed / flipDuration;
            float eased = flipCurve.Evaluate(t);
            
            Quaternion currRot = Quaternion.Slerp(startRot, rot3D, eased);
            Vector3 forward = currRot * Vector3.forward;
            
            Vector3 baseOffset = Vector3.Lerp(baseOffset2D, baseOffset3D, eased);
            
            Vector3 lateral = baseOffset - Vector3.Project(baseOffset, -forward);
            
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
        
        Vector3 endPos;
        if (currentTarget != null)
            endPos = currentTarget.position + positionOffset3D;
        else
            endPos = positionOffset3D;

        transform.position = endPos;
        transform.rotation = rot3D;
        cam.orthographic = false;
        cam.fieldOfView = baseFov;


        isFlipping = false;

    }
}
