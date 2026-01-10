using System;
using System.Collections;
using UnityEngine;

public class LevelUpPrefabsIntro : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private Vector3 targetScale = Vector3.one;

    [Header("Timings (seconds)")]
    [Min(0f)][SerializeField] private float intro = 0.25f;
    [Min(0f)][SerializeField] private float stay = 1.0f;
    [Min(0f)][SerializeField] private float outro = 0.25f;

    [Header("Options")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = false;

    public GameObject target;

    private Coroutine routine;

    public void Init(GameObject calledTarget)
    {
        target = calledTarget;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void FixedUpdate()
    {
        if (!target) return;
        
        transform.position = target.transform.position;
        transform.rotation = Quaternion.Euler(gameObject.transform.rotation.x, target.transform.rotation.y, gameObject.transform.rotation.z);
    }

    public void Play()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        transform.localScale = Vector3.zero;

        yield return ScaleOverTime(Vector3.zero, targetScale, intro);

        // Stay
        if (stay > 0f)
            yield return Wait(stay);

        // Outro: targetScale -> 0
        yield return ScaleOverTime(targetScale, Vector3.zero, outro);

        // Destroy at end
        Destroy(gameObject);
    }

    private IEnumerator ScaleOverTime(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            transform.localScale = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime();
            float a = Mathf.Clamp01(t / duration);
            transform.localScale = Vector3.LerpUnclamped(from, to, a);
            yield return null;
        }

        transform.localScale = to;
    }

    private IEnumerator Wait(float duration)
    {
        if (duration <= 0f) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime();
            yield return null;
        }
    }

    private float DeltaTime() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
}
