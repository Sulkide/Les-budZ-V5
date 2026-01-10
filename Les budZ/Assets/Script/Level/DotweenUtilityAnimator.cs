using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DotweenUtilityAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Defaults")]
    [SerializeField] private float generalForce = 1f;
    [SerializeField] private float generalSpeed = 1f;
    [SerializeField] private float generalBeat = 1f;
    [SerializeField] private bool randomizeStartPhase = true;

    [Header("Debug")]
    [SerializeField] private Transform baseTransform;

    [Header("MOVE / Circle (XY)")]
    public bool MoveCircleOnZ = false;
    [SerializeField] private float rayon = 0f;
    [SerializeField] private float circleCustomSpeed = 0f;
    [SerializeField] private bool circleClockwise = true;

    [Header("MOVE / Linear / X")]
    public bool MoveLinearOnX = false;
    [SerializeField] private float moveLinearXCustomForce = 0f;
    [SerializeField] private float moveLinearXCustomSpeed = 0f;

    [Header("MOVE / Linear / Y")]
    public bool MoveLinearOnY = false;
    [SerializeField] private float moveLinearYCustomForce = 0f;
    [SerializeField] private float moveLinearYCustomSpeed = 0f;

    [Header("MOVE / Linear / Z")]
    public bool MoveLinearOnZ = false;
    [SerializeField] private float moveLinearZCustomForce = 0f;
    [SerializeField] private float moveLinearZCustomSpeed = 0f;

    [Header("MOVE / Flotte / X")]
    public bool MoveFlotteOnX = false;
    [SerializeField] private float moveFlotteXRange = 0f;
    [SerializeField] private float moveFlotteXCustomSpeed = 0f;
    [SerializeField] private float moveFlotteXCustomForce = 0f;

    [Header("MOVE / Flotte / Y")]
    public bool MoveFlotteOnY = false;
    [SerializeField] private float moveFlotteYRange = 0f;
    [SerializeField] private float moveFlotteYCustomSpeed = 0f;
    [SerializeField] private float moveFlotteYCustomForce = 0f;

    [Header("MOVE / Flotte / Z")]
    public bool MoveFlotteOnZ = false;
    [SerializeField] private float moveFlotteZRange = 0f;
    [SerializeField] private float moveFlotteZCustomSpeed = 0f;
    [SerializeField] private float moveFlotteZCustomForce = 0f;

    [Header("ROTATION / Continuous / X")]
    public bool RotateOnX = false;
    [SerializeField] private float rotateXCustomSpeed = 0f;
    [SerializeField] private bool rotateXClockwise = true;

    [Header("ROTATION / Continuous / Y")]
    public bool RotateOnY = false;
    [SerializeField] private float rotateYCustomSpeed = 0f;
    [SerializeField] private bool rotateYClockwise = true;

    [Header("ROTATION / Continuous / Z")]
    public bool RotateOnZ = false;
    [SerializeField] private float rotateZCustomSpeed = 0f;
    [SerializeField] private bool rotateZClockwise = true;

    [Header("ROTATION / A->B Linear / X")]
    public bool RotateAToBLinearOnX = false;
    [SerializeField] private float rotateABLinearX_A = 0f;
    [SerializeField] private float rotateABLinearX_B = 90f;
    [SerializeField] private float rotateABLinearXCustomSpeed = 0f;

    [Header("ROTATION / A->B Linear / Y")]
    public bool RotateAToBLinearOnY = false;
    [SerializeField] private float rotateABLinearY_A = 0f;
    [SerializeField] private float rotateABLinearY_B = 90f;
    [SerializeField] private float rotateABLinearYCustomSpeed = 0f;

    [Header("ROTATION / A->B Linear / Z")]
    public bool RotateAToBLinearOnZ = false;
    [SerializeField] private float rotateABLinearZ_A = 0f;
    [SerializeField] private float rotateABLinearZ_B = 90f;
    [SerializeField] private float rotateABLinearZCustomSpeed = 0f;

    [Header("ROTATION / A->B Flotte / X")]
    public bool RotateAToBFlotteOnX = false;
    [SerializeField] private float rotateABFlotteX_A = 0f;
    [SerializeField] private float rotateABFlotteX_B = 90f;
    [SerializeField] private float rotateABFlotteXCustomSpeed = 0f;
    [SerializeField] private float rotateABFlotteXCustomForce = 0f;

    [Header("ROTATION / A->B Flotte / Y")]
    public bool RotateAToBFlotteOnY = false;
    [SerializeField] private float rotateABFlotteY_A = 0f;
    [SerializeField] private float rotateABFlotteY_B = 90f;
    [SerializeField] private float rotateABFlotteYCustomSpeed = 0f;
    [SerializeField] private float rotateABFlotteYCustomForce = 0f;

    [Header("ROTATION / A->B Flotte / Z")]
    public bool RotateAToBFlotteOnZ = false;
    [SerializeField] private float rotateABFlotteZ_A = 0f;
    [SerializeField] private float rotateABFlotteZ_B = 90f;
    [SerializeField] private float rotateABFlotteZCustomSpeed = 0f;
    [SerializeField] private float rotateABFlotteZCustomForce = 0f;

    [Header("SCALE / Crush Linear / X")]
    public bool ScaleCrushLinearOnX = false;
    [SerializeField] private float scaleCrushXCustomForce = 0f;
    [SerializeField] private float scaleCrushXCustomSpeed = 0f;
    [SerializeField] private float scaleCrushXCustomBeat = 0f;

    [Header("SCALE / Crush Linear / Y")]
    public bool ScaleCrushLinearOnY = false;
    [SerializeField] private float scaleCrushYCustomForce = 0f;
    [SerializeField] private float scaleCrushYCustomSpeed = 0f;
    [SerializeField] private float scaleCrushYCustomBeat = 0f;

    [Header("SCALE / Crush Linear / Z")]
    public bool ScaleCrushLinearOnZ = false;
    [SerializeField] private float scaleCrushZCustomForce = 0f;
    [SerializeField] private float scaleCrushZCustomSpeed = 0f;
    [SerializeField] private float scaleCrushZCustomBeat = 0f;

    [Header("SCALE / Expand Linear / X")]
    public bool ScaleExpendLinearOnX = false;
    [SerializeField] private float scaleExpandXCustomForce = 0f;
    [SerializeField] private float scaleExpandXCustomSpeed = 0f;
    [SerializeField] private float scaleExpandXCustomBeat = 0f;

    [Header("SCALE / Expand Linear / Y")]
    public bool ScaleExpendLinearOnY = false;
    [SerializeField] private float scaleExpandYCustomForce = 0f;
    [SerializeField] private float scaleExpandYCustomSpeed = 0f;
    [SerializeField] private float scaleExpandYCustomBeat = 0f;

    [Header("SCALE / Expand Linear / Z")]
    public bool ScaleExpendLinearOnZ = false;
    [SerializeField] private float scaleExpandZCustomForce = 0f;
    [SerializeField] private float scaleExpandZCustomSpeed = 0f;
    [SerializeField] private float scaleExpandZCustomBeat = 0f;

    [Header("SCALE / Squish")]
    public bool ScaleSquish = false;
    [SerializeField] private float scaleSquishCustomForce = 0f;
    [SerializeField] private float scaleSquishCustomSpeed = 0f;
    [SerializeField] private float scaleSquishCustomBeat = 0f;

    private readonly List<Tween> _tweens = new();
    private Transform T => target != null ? target : transform;

    private Vector3 _center;
    private float _baseZ;
    private Vector3 _baseScale;

    private bool _hasMoveActive;

    private Vector3 _moveOffsetCircle;
    private Vector3 _moveOffsetLinearX, _moveOffsetLinearY, _moveOffsetLinearZ;
    private Vector3 _moveOffsetFlotteX, _moveOffsetFlotteY, _moveOffsetFlotteZ;

    private void Start()
    {
        CacheBaseTransform();
        PlayActiveAnimations();
    }

    private void OnEnable()
    {
        if (baseTransform != null) PlayActiveAnimations();
    }

    private void OnDisable() => KillTweens();

    private void OnDestroy()
    {
        KillTweens();
        if (baseTransform != null)
        {
            Destroy(baseTransform.gameObject);
            baseTransform = null;
        }
    }

    private void LateUpdate()
    {
        if (!_hasMoveActive) return;

        Vector3 total =
            _moveOffsetCircle +
            _moveOffsetLinearX + _moveOffsetLinearY + _moveOffsetLinearZ +
            _moveOffsetFlotteX + _moveOffsetFlotteY + _moveOffsetFlotteZ;

        T.position = _center + total;
    }

    private void CacheBaseTransform()
    {
        if (baseTransform != null) return;
        var go = new GameObject($"{name}_BaseTransform");
        go.hideFlags = HideFlags.HideInHierarchy;
        baseTransform = go.transform;
    }

    private void CaptureBaseNow()
    {
        baseTransform.position = T.position;
        baseTransform.rotation = T.rotation;
        baseTransform.localScale = T.localScale;

        _center = T.position;
        _baseZ = T.position.z;
        _baseScale = T.localScale;

        _hasMoveActive = false;
        _moveOffsetCircle = Vector3.zero;
        _moveOffsetLinearX = Vector3.zero;
        _moveOffsetLinearY = Vector3.zero;
        _moveOffsetLinearZ = Vector3.zero;
        _moveOffsetFlotteX = Vector3.zero;
        _moveOffsetFlotteY = Vector3.zero;
        _moveOffsetFlotteZ = Vector3.zero;
    }

    private void PlayActiveAnimations()
    {
        KillTweens();
        CaptureBaseNow();

        if (MoveCircleOnZ) PlayMoveCircle();

        if (MoveLinearOnX) PlayMoveLinear(Axis.X, moveLinearXCustomForce, moveLinearXCustomSpeed);
        if (MoveLinearOnY) PlayMoveLinear(Axis.Y, moveLinearYCustomForce, moveLinearYCustomSpeed);
        if (MoveLinearOnZ) PlayMoveLinear(Axis.Z, moveLinearZCustomForce, moveLinearZCustomSpeed);

        if (MoveFlotteOnX) PlayMoveFlotte(Axis.X, moveFlotteXRange, moveFlotteXCustomSpeed, moveFlotteXCustomForce);
        if (MoveFlotteOnY) PlayMoveFlotte(Axis.Y, moveFlotteYRange, moveFlotteYCustomSpeed, moveFlotteYCustomForce);
        if (MoveFlotteOnZ) PlayMoveFlotte(Axis.Z, moveFlotteZRange, moveFlotteZCustomSpeed, moveFlotteZCustomForce);

        if (RotateOnX) PlayRotateContinuous(Axis.X, rotateXCustomSpeed, rotateXClockwise);
        if (RotateOnY) PlayRotateContinuous(Axis.Y, rotateYCustomSpeed, rotateYClockwise);
        if (RotateOnZ) PlayRotateContinuous(Axis.Z, rotateZCustomSpeed, rotateZClockwise);

        if (RotateAToBLinearOnX) PlayRotateAToBLinear(Axis.X, rotateABLinearX_A, rotateABLinearX_B, rotateABLinearXCustomSpeed);
        if (RotateAToBLinearOnY) PlayRotateAToBLinear(Axis.Y, rotateABLinearY_A, rotateABLinearY_B, rotateABLinearYCustomSpeed);
        if (RotateAToBLinearOnZ) PlayRotateAToBLinear(Axis.Z, rotateABLinearZ_A, rotateABLinearZ_B, rotateABLinearZCustomSpeed);

        if (RotateAToBFlotteOnX) PlayRotateAToBFlotte(Axis.X, rotateABFlotteX_A, rotateABFlotteX_B, rotateABFlotteXCustomSpeed, rotateABFlotteXCustomForce);
        if (RotateAToBFlotteOnY) PlayRotateAToBFlotte(Axis.Y, rotateABFlotteY_A, rotateABFlotteY_B, rotateABFlotteYCustomSpeed, rotateABFlotteYCustomForce);
        if (RotateAToBFlotteOnZ) PlayRotateAToBFlotte(Axis.Z, rotateABFlotteZ_A, rotateABFlotteZ_B, rotateABFlotteZCustomSpeed, rotateABFlotteZCustomForce);

        if (ScaleCrushLinearOnX) PlayScalePulseAxis(Axis.X, -GetForce(scaleCrushXCustomForce), scaleCrushXCustomSpeed, scaleCrushXCustomBeat);
        if (ScaleCrushLinearOnY) PlayScalePulseAxis(Axis.Y, -GetForce(scaleCrushYCustomForce), scaleCrushYCustomSpeed, scaleCrushYCustomBeat);
        if (ScaleCrushLinearOnZ) PlayScalePulseAxis(Axis.Z, -GetForce(scaleCrushZCustomForce), scaleCrushZCustomSpeed, scaleCrushZCustomBeat);

        if (ScaleExpendLinearOnX) PlayScalePulseAxis(Axis.X, GetForce(scaleExpandXCustomForce), scaleExpandXCustomSpeed, scaleExpandXCustomBeat);
        if (ScaleExpendLinearOnY) PlayScalePulseAxis(Axis.Y, GetForce(scaleExpandYCustomForce), scaleExpandYCustomSpeed, scaleExpandYCustomBeat);
        if (ScaleExpendLinearOnZ) PlayScalePulseAxis(Axis.Z, GetForce(scaleExpandZCustomForce), scaleExpandZCustomSpeed, scaleExpandZCustomBeat);

        if (ScaleSquish) PlayScaleSquish(scaleSquishCustomForce, scaleSquishCustomSpeed, scaleSquishCustomBeat);
    }

    private void PlayMoveCircle()
    {
        _hasMoveActive = true;

        float speed = GetSpeed(circleCustomSpeed);
        float r = Mathf.Abs(rayon) > 0f ? Mathf.Abs(rayon) : Mathf.Abs(generalForce);
        r = Mathf.Max(0f, r);

        float duration = 360f / Mathf.Max(0.0001f, speed);
        float dir = circleClockwise ? -1f : 1f;

        float angle = 0f;

        Tween tw = DOTween
            .To(() => angle, x => angle = x, dir * 360f, duration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .OnUpdate(() =>
            {
                float rad = angle * Mathf.Deg2Rad;
                _moveOffsetCircle = new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);
            });

        if (randomizeStartPhase)
        {
            float t = Random.Range(0f, duration);
            tw.Goto(t, true);
        }

        _tweens.Add(tw);
    }

    private void PlayMoveLinear(Axis axis, float customForce, float customSpeed)
    {
        _hasMoveActive = true;

        float dist = Mathf.Abs(GetForce(customForce));
        float speed = Mathf.Max(0.0001f, GetSpeed(customSpeed));
        if (dist <= 0f) return;

        float oneWay = (2f * dist) / speed;
        float total = Mathf.Max(0.0001f, 2f * oneWay);

        float time = 0f;
        Vector3 axisV = AxisVector(axis);

        Tween tw = DOTween
            .To(() => time, x => time = x, total, total)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .OnUpdate(() =>
            {
                float u = LinearPingPong01(time, oneWay);
                float scalar = Mathf.Lerp(dist, -dist, u);
                Vector3 off = axisV * scalar;

                if (axis == Axis.X) _moveOffsetLinearX = off;
                else if (axis == Axis.Y) _moveOffsetLinearY = off;
                else _moveOffsetLinearZ = off;
            });

        float startTime = randomizeStartPhase ? Random.Range(0f, total) : total * 0.25f;
        tw.Goto(startTime, true);

        _tweens.Add(tw);
    }

    private void PlayMoveFlotte(Axis axis, float range, float customSpeed, float customForce)
    {
        _hasMoveActive = true;

        float dist = Mathf.Abs(range) > 0f ? Mathf.Abs(range) : Mathf.Abs(generalForce);
        if (dist <= 0f) return;

        float speed = Mathf.Max(0.0001f, GetSpeed(customSpeed));
        float oneWay = (2f * dist) / speed;

        float slow = Mathf.Abs(customForce) > 0f ? Mathf.Abs(customForce) : Mathf.Abs(generalForce);
        float smoothAmount = SlowToSmoothAmount(slow);

        float t = 0f;
        Vector3 axisV = AxisVector(axis);

        Tween tw = DOTween
            .To(() => t, x => t = x, 1f, oneWay)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo)
            .OnUpdate(() =>
            {
                float u = BlendSmoothNoStop(t, smoothAmount);
                float scalar = Mathf.Lerp(dist, -dist, u);
                Vector3 off = axisV * scalar;

                if (axis == Axis.X) _moveOffsetFlotteX = off;
                else if (axis == Axis.Y) _moveOffsetFlotteY = off;
                else _moveOffsetFlotteZ = off;
            });

        if (randomizeStartPhase)
        {
            float cycle = 2f * oneWay;
            tw.Goto(Random.Range(0f, cycle), true);
        }

        _tweens.Add(tw);
    }

    private void PlayRotateContinuous(Axis axis, float customSpeed, bool clockwise)
    {
        float speed = Mathf.Max(0.0001f, GetSpeed(customSpeed));
        float duration = 360f / speed;
        float dir = clockwise ? -1f : 1f;

        Vector3 delta = axis switch
        {
            Axis.X => new Vector3(dir * 360f, 0f, 0f),
            Axis.Y => new Vector3(0f, dir * 360f, 0f),
            _ => new Vector3(0f, 0f, dir * 360f)
        };

        Tween tw = T
            .DORotate(delta, duration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);

        if (randomizeStartPhase)
        {
            float t = Random.Range(0f, duration);
            tw.Goto(t, true);
        }

        _tweens.Add(tw);
    }

    private void PlayRotateAToBLinear(Axis axis, float aDeg, float bDeg, float customSpeed)
    {
        float speed = Mathf.Max(0.0001f, GetSpeed(customSpeed));
        float delta = Mathf.Abs(bDeg - aDeg);
        float oneWay = Mathf.Max(0.0001f, delta / speed);
        float total = 2f * oneWay;

        float time = 0f;

        Tween tw = DOTween
            .To(() => time, x => time = x, total, total)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .OnUpdate(() =>
            {
                float tNorm = LinearPingPong01(time, oneWay);
                float ang = Mathf.LerpUnclamped(aDeg, bDeg, tNorm);
                ApplyAxisEuler(axis, ang);
            });

        float startTime = randomizeStartPhase ? Random.Range(0f, total) : total * 0.5f;
        tw.Goto(startTime, true);

        _tweens.Add(tw);
    }

    private void PlayRotateAToBFlotte(Axis axis, float aDeg, float bDeg, float customSpeed, float customForce)
    {
        float speed = Mathf.Max(0.0001f, GetSpeed(customSpeed));
        float delta = Mathf.Abs(bDeg - aDeg);
        if (delta <= 0f) return;

        float oneWay = Mathf.Max(0.0001f, delta / speed);

        float slow = Mathf.Abs(customForce) > 0f ? Mathf.Abs(customForce) : Mathf.Abs(generalForce);
        float smoothAmount = SlowToSmoothAmount(slow);

        float t = 0f;

        Tween tw = DOTween
            .To(() => t, x => t = x, 1f, oneWay)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo)
            .OnUpdate(() =>
            {
                float u = BlendSmoothNoStop(t, smoothAmount);
                float ang = Mathf.LerpUnclamped(aDeg, bDeg, u);
                ApplyAxisEuler(axis, ang);
            });

        if (randomizeStartPhase)
        {
            float cycle = 2f * oneWay;
            tw.Goto(Random.Range(0f, cycle), true);
        }

        _tweens.Add(tw);
    }

    private void PlayScalePulseAxis(Axis axis, float deltaScale, float customSpeed, float customBeat)
    {
        float mag = Mathf.Abs(deltaScale);
        if (mag <= 0f) return;

        float speed = Mathf.Max(0.0001f, GetSpeed(customSpeed));
        float beat = Mathf.Max(0.0001f, GetBeat(customBeat));

        float half = Mathf.Max(0.0001f, mag / speed);
        float active = 2f * half;
        float period = Mathf.Max(beat, active);

        Vector3 targetScale = _baseScale;
        targetScale = SetAxis(targetScale, axis, Mathf.Max(0.01f, GetAxis(_baseScale, axis) + deltaScale));

        float time = 0f;

        Tween tw = DOTween
            .To(() => time, x => time = x, period, period)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .OnUpdate(() =>
            {
                Vector3 s = _baseScale;

                if (time < half)
                {
                    float t = time / half;
                    s = Vector3.LerpUnclamped(_baseScale, targetScale, t);
                }
                else if (time < active)
                {
                    float t = (time - half) / half;
                    s = Vector3.LerpUnclamped(targetScale, _baseScale, t);
                }

                T.localScale = s;
            });

        float startTime = randomizeStartPhase ? Random.Range(0f, period) : 0f;
        tw.Goto(startTime, true);

        _tweens.Add(tw);
    }

    private void PlayScaleSquish(float customForce, float customSpeed, float customBeat)
    {
        float force = Mathf.Abs(GetForce(customForce));
        if (force <= 0f) return;

        float speed = Mathf.Max(0.0001f, GetSpeed(customSpeed));
        float beat = Mathf.Max(0.0001f, GetBeat(customBeat));

        float half = Mathf.Max(0.0001f, force / speed);
        float active = 2f * half;
        float period = Mathf.Max(beat, active);

        Vector3 a = _baseScale;
        Vector3 b = _baseScale;

        b.x = Mathf.Max(0.01f, _baseScale.x + force);
        b.y = Mathf.Max(0.01f, _baseScale.y - force);
        b.z = Mathf.Max(0.01f, _baseScale.z);

        float time = 0f;

        Tween tw = DOTween
            .To(() => time, x => time = x, period, period)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .OnUpdate(() =>
            {
                Vector3 s = _baseScale;

                if (time < half)
                {
                    float t = time / half;
                    s = Vector3.LerpUnclamped(a, b, t);
                }
                else if (time < active)
                {
                    float t = (time - half) / half;
                    s = Vector3.LerpUnclamped(b, a, t);
                }

                T.localScale = s;
            });

        float startTime = randomizeStartPhase ? Random.Range(0f, period) : 0f;
        tw.Goto(startTime, true);

        _tweens.Add(tw);
    }

    private void ApplyAxisEuler(Axis axis, float angleDeg)
    {
        Vector3 e = T.localEulerAngles;
        e = axis switch
        {
            Axis.X => new Vector3(angleDeg, e.y, e.z),
            Axis.Y => new Vector3(e.x, angleDeg, e.z),
            _ => new Vector3(e.x, e.y, angleDeg)
        };
        T.localEulerAngles = e;
    }

    private static float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static float BlendSmoothNoStop(float t, float smoothAmount)
    {
        t = Mathf.Clamp01(t);
        smoothAmount = Mathf.Clamp01(smoothAmount);
        float s = SmoothStep01(t);
        return Mathf.LerpUnclamped(t, s, smoothAmount);
    }

    private static float SlowToSmoothAmount(float slow)
    {
        slow = Mathf.Max(0f, slow);
        float a = slow / (slow + 1f);
        return Mathf.Clamp(a, 0f, 0.95f);
    }

    private float GetSpeed(float custom) => Mathf.Abs(custom) > 0f ? Mathf.Abs(custom) : Mathf.Max(0.0001f, generalSpeed);
    private float GetForce(float custom) => Mathf.Abs(custom) > 0f ? Mathf.Abs(custom) : Mathf.Abs(generalForce);
    private float GetBeat(float custom) => custom > 0f ? custom : Mathf.Max(0.0001f, generalBeat);

    private static float LinearPingPong01(float time, float oneWay)
    {
        if (oneWay <= 0f) return 0f;
        if (time < oneWay) return Mathf.Clamp01(time / oneWay);
        float t = (time - oneWay) / oneWay;
        return 1f - Mathf.Clamp01(t);
    }

    private static Vector3 AxisVector(Axis axis) => axis switch
    {
        Axis.X => Vector3.right,
        Axis.Y => Vector3.up,
        _ => Vector3.forward
    };

    private static float GetAxis(Vector3 v, Axis axis) => axis switch
    {
        Axis.X => v.x,
        Axis.Y => v.y,
        _ => v.z
    };

    private static Vector3 SetAxis(Vector3 v, Axis axis, float value)
    {
        if (axis == Axis.X) v.x = value;
        else if (axis == Axis.Y) v.y = value;
        else v.z = value;
        return v;
    }

    private void KillTweens()
    {
        for (int i = 0; i < _tweens.Count; i++)
        {
            if (_tweens[i] != null && _tweens[i].IsActive()) _tweens[i].Kill();
        }
        _tweens.Clear();

        _hasMoveActive = false;
        _moveOffsetCircle = Vector3.zero;
        _moveOffsetLinearX = Vector3.zero;
        _moveOffsetLinearY = Vector3.zero;
        _moveOffsetLinearZ = Vector3.zero;
        _moveOffsetFlotteX = Vector3.zero;
        _moveOffsetFlotteY = Vector3.zero;
        _moveOffsetFlotteZ = Vector3.zero;
    }

    private enum Axis { X, Y, Z }

    [ContextMenu("Replay Animations")]
    public void ReplayAnimations()
    {
        CacheBaseTransform();
        PlayActiveAnimations();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        PlayActiveAnimations();
    }
#endif
}
