using System.Collections;
using UnityEngine;

public class DimensionFlipObserver : MonoBehaviour
{
    [Header("Réglages du flip")]
    [Tooltip("Durée de l’animation de rotation (en secondes)")]
    public float flipDuration = 0.5f;

    [Tooltip("Axe local autour duquel on tourne (par défaut Y)")]
    public Vector3 localAxis = Vector3.up;

    [Tooltip("Angle en 2D (en degrés)")]
    public float angle2D = 0f;

    [Tooltip("Angle en 3D (en degrés)")]
    public float angle3D = 90f;

    [Tooltip("Utiliser la rotation locale (true) ou globale (false)")]
    public bool useLocalRotation = true;

    private Quaternion baseRotation;
    private Coroutine currentRoutine;
    private Coroutine subscribeRoutine;

    private void Awake()
    {
        baseRotation = useLocalRotation ? transform.localRotation : transform.rotation;
    }

    private void OnEnable()
    {
        subscribeRoutine = StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnDimensionChanged -= HandleDimensionChanged;
        }
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Attendre que GameManager.instance soit non-null
        while (GameManager.instance == null)
            yield return null;

        // Sécurité au cas où on serait déjà abonné
        GameManager.instance.OnDimensionChanged -= HandleDimensionChanged;
        GameManager.instance.OnDimensionChanged += HandleDimensionChanged;

        // Sync immédiate à l’état courant (2D ou 3D)
        HandleDimensionChanged(GameManager.instance.is3d);
        subscribeRoutine = null;
    }

    private void HandleDimensionChanged(bool is3D)
    {
        float targetAngle = is3D ? angle3D : angle2D;

        Quaternion targetRot =
            baseRotation * Quaternion.AngleAxis(targetAngle, localAxis);

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(RotateTo(targetRot));
    }

    private IEnumerator RotateTo(Quaternion targetRot)
    {
        Quaternion start = useLocalRotation ? transform.localRotation : transform.rotation;
        float elapsed = 0f;

        if (flipDuration <= 0f)
        {
            if (useLocalRotation)
                transform.localRotation = targetRot;
            else
                transform.rotation = targetRot;

            currentRoutine = null;
            yield break;
        }

        while (elapsed < flipDuration)
        {
            float t = elapsed / flipDuration;
            if (useLocalRotation)
                transform.localRotation = Quaternion.Slerp(start, targetRot, t);
            else
                transform.rotation = Quaternion.Slerp(start, targetRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (useLocalRotation)
            transform.localRotation = targetRot;
        else
            transform.rotation = targetRot;

        currentRoutine = null;
    }
}
