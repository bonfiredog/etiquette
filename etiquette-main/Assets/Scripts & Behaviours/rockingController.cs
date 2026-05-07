using System.Collections;
using UnityEngine;

public class rockingController : MonoBehaviour
{
    [Header("Speed-Based Swaying Motion")]
    [SerializeField] private float baseSwayAmount = 2f;      // Max degrees of Z-axis rotation at full speed
    [SerializeField] private float minSwayAmount = 0.2f;     // Min degrees when stationary/slow
    [SerializeField] private float baseSwaySpeed = 0.8f;     // Speed of swaying motion at full speed
    [SerializeField] private float minSwaySpeed = 0.1f;      // Min sway speed when stationary/slow
    [SerializeField] private bool enableSway = true;

    [Header("Speed-Based Ambient Random Jolts")]
    [SerializeField] private bool enableAmbientJolts = true;
    [SerializeField] private float baseJoltMinInterval = 1f;     // Minimum time between jolts at full speed
    [SerializeField] private float baseJoltMaxInterval = 4f;     // Maximum time between jolts at full speed
    [SerializeField] private float slowJoltMinInterval = 8f;     // Minimum time between jolts when slow
    [SerializeField] private float slowJoltMaxInterval = 20f;    // Maximum time between jolts when slow

    [Header("Ambient Jolt Strength")]
    [SerializeField] private Vector2 baseForwardBackwardRange = new Vector2(-0.4f, 0.3f);  // X-axis movement at full speed
    [SerializeField] private Vector2 baseUpDownRange = new Vector2(-0.2f, 0.5f);           // Y-axis movement at full speed
    [SerializeField] private Vector2 baseLeftRightRange = new Vector2(-0.3f, 0.3f);        // Z-axis movement at full speed
    [SerializeField] private float minJoltMultiplier = 0.1f;                                // Minimum jolt strength when slow

    [Header("Ambient Jolt Timing")]
    [SerializeField] private float ambientJoltDuration = 0.3f;
    [SerializeField] private AnimationCurve ambientJoltCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sudden Jolt")]
    [SerializeField] private float suddenJoltDuration = 0.3f;
    [SerializeField] private Vector2 suddenJoltForwardBackward = new Vector2(-0.5f, 0.5f);
    [SerializeField] private Vector2 suddenJoltUpDown = new Vector2(-0.3f, 0.6f);
    [SerializeField] private Vector2 suddenJoltLeftRight = new Vector2(-0.4f, 0.4f);
    [SerializeField] private float suddenJoltStrength = 1f;
    [SerializeField] private AnimationCurve suddenJoltCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
[SerializeField] private float suddenJoltJiggleFrequency = 8f;   // How fast it oscillates
[SerializeField] private float suddenJoltJiggleDamping = 4f;     // How quickly it dies out
[SerializeField] private float suddenJoltJiggleDuration = 0.8f; 
[SerializeField] private float suddenJoltJiggleAmplitude = 50f;

    [Header("References")]
    public GameObject tcObj;
    public GameObject cameraObj;

    private TrainControl tc;
    private cameraControl cc;

    // Original transform state
    private Vector3 originalPosition;
    private Vector3 originalRotation;
    private float swayOffset;

    // Cached speed data
    private float currentSpeedPercentage;
    private float currentSwayAmount;
    private float currentSwaySpeed;

    // Ambient jolt state
    private float nextAmbientJoltTime;
    private bool isAmbientJolting = false;
    private float ambientJoltStartTime;
    private Vector3 ambientJoltTargetOffset;
    private Vector3 currentAmbientJoltOffset;

    // Sudden jolt state
    private bool isSuddenJolting = false;
    private float suddenJoltStartTime;
    private float currentSuddenJoltDuration;
    private Vector3 suddenJoltTargetOffset;
    private Vector3 currentSuddenJoltOffset;
    private Coroutine suddenJoltCoroutine;
    

private float suddenJoltMagnitude;
private bool isJiggling = false;

private float jolttimer = 10;

    private void Start()
    {
        if (cameraObj != null) cc = cameraObj.GetComponent<cameraControl>();
        if (tcObj != null) tc = tcObj.GetComponent<TrainControl>();

        originalPosition = transform.localPosition;
        originalRotation = transform.localEulerAngles;

        // Randomize sway phase so multiple carriages don't sync
        swayOffset = Random.Range(0f, Mathf.PI * 2f);

        UpdateSpeedPercentage();
        ScheduleNextAmbientJolt();
    }

    private void Update()
    {

        //Randomised jolting
        if (tc.trainCurrentSpeed > 0) {
        if (jolttimer > 0) {
            jolttimer -= 1 * Time.deltaTime;
        } else {
            SuddenJolt();
            jolttimer = Random.Range(5,40);
        }
        }



        UpdateSpeedPercentage();

        bool allowAmbientMotion = cc != null && cc.outAmount > 0f;
        bool trainMovingEnoughForAmbient = tc != null && tc.trainCurrentSpeed > 2f;

        // SWAY: only when outAmount > 0 and train is moving enough
        if (enableSway && allowAmbientMotion && trainMovingEnoughForAmbient)
        {
            UpdateSway();
        }
        else
        {
            ResetRotation();
        }

        // AMBIENT RANDOM JOLTS:
        // only allowed to START and run when outAmount > 0 and train is moving enough
        if (enableAmbientJolts && allowAmbientMotion && trainMovingEnoughForAmbient)
        {
            UpdateAmbientJoltTrigger();
            UpdateAmbientJoltPlayback();
        }
        else
        {
            StopAmbientJolt();
        }

        // SUDDEN JOLTS:
        // always update regardless of outAmount / train speed
        

        ApplyMotion();
    }

    private void UpdateSpeedPercentage()
    {
        if (tc != null && tc.trainTopSpeed > 0f)
        {
            currentSpeedPercentage = Mathf.Clamp01(tc.trainCurrentSpeed / tc.trainTopSpeed);
        }
        else
        {
            currentSpeedPercentage = 0.5f;
        }

        currentSwayAmount = Mathf.Lerp(minSwayAmount, baseSwayAmount, currentSpeedPercentage);
        currentSwaySpeed = Mathf.Lerp(minSwaySpeed, baseSwaySpeed, currentSpeedPercentage);
    }

    private void UpdateSway()
    {
        float primarySway = Mathf.Sin((Time.time + swayOffset) * currentSwaySpeed) * currentSwayAmount;
        float secondarySway = Mathf.Sin((Time.time + swayOffset) * currentSwaySpeed * 1.7f) * (currentSwayAmount * 0.3f);
        float totalSway = primarySway + secondarySway;

        transform.localRotation = Quaternion.Euler(
            originalRotation.x,
            originalRotation.y,
            originalRotation.z + totalSway
        );
    }

    private void ResetRotation()
    {
        transform.localRotation = Quaternion.Euler(originalRotation);
    }

    private void UpdateAmbientJoltTrigger()
    {
        if (!isAmbientJolting && Time.time >= nextAmbientJoltTime)
        {
            StartAmbientJolt();
        }
    }

    private void StartAmbientJolt()
    {
        isAmbientJolting = true;
        ambientJoltStartTime = Time.time;

        float joltMultiplier = Mathf.Lerp(minJoltMultiplier, 1f, currentSpeedPercentage);

        ambientJoltTargetOffset = new Vector3(
            Random.Range(baseForwardBackwardRange.x, baseForwardBackwardRange.y) * joltMultiplier,
            Random.Range(baseUpDownRange.x, baseUpDownRange.y) * joltMultiplier,
            Random.Range(baseLeftRightRange.x, baseLeftRightRange.y) * joltMultiplier
        );
    }

    private void UpdateAmbientJoltPlayback()
    {
        if (!isAmbientJolting)
        {
            currentAmbientJoltOffset = Vector3.zero;
            return;
        }

        float progress = (Time.time - ambientJoltStartTime) / Mathf.Max(0.0001f, ambientJoltDuration);

        if (progress >= 1f)
        {
            isAmbientJolting = false;
            currentAmbientJoltOffset = Vector3.zero;
            ScheduleNextAmbientJolt();
            return;
        }

        currentAmbientJoltOffset = EvaluatePingPongJolt(
            ambientJoltTargetOffset,
            progress,
            ambientJoltCurve
        );
    }

    private void StopAmbientJolt()
    {
        isAmbientJolting = false;
        currentAmbientJoltOffset = Vector3.zero;
    }

    private void ScheduleNextAmbientJolt()
    {
        float minInterval = Mathf.Lerp(slowJoltMinInterval, baseJoltMinInterval, currentSpeedPercentage);
        float maxInterval = Mathf.Lerp(slowJoltMaxInterval, baseJoltMaxInterval, currentSpeedPercentage);

        nextAmbientJoltTime = Time.time + Random.Range(minInterval, maxInterval);
    }


public void SuddenJolt(float strengthOverride = -1f, float durationOverride = -1f)
{
    float strength = strengthOverride >= 0f ? strengthOverride : suddenJoltStrength;
    float duration = durationOverride > 0f ? durationOverride : suddenJoltDuration;

    suddenJoltTargetOffset = new Vector3(
        Random.Range(suddenJoltForwardBackward.x, suddenJoltForwardBackward.y) * strength,
        Random.Range(suddenJoltUpDown.x, suddenJoltUpDown.y) * strength,
        Random.Range(suddenJoltLeftRight.x, suddenJoltLeftRight.y) * strength
    );

    suddenJoltMagnitude = suddenJoltTargetOffset.magnitude; // add this line

    currentSuddenJoltDuration = duration;
    suddenJoltStartTime = Time.time;
    isSuddenJolting = true;

    if (suddenJoltCoroutine != null)
        StopCoroutine(suddenJoltCoroutine);

    suddenJoltCoroutine = StartCoroutine(SuddenJoltThenJiggle(duration));
}

private IEnumerator SuddenJoltThenJiggle(float duration)
{
    isSuddenJolting = true;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        currentSuddenJoltOffset = EvaluatePingPongJolt(suddenJoltTargetOffset, progress, suddenJoltCurve);
        yield return null;
    }

    isSuddenJolting = false;
    elapsed = 0f;

    Vector3 jiggleAxis = suddenJoltTargetOffset.normalized * (suddenJoltTargetOffset.magnitude * 0.5f);

    while (elapsed < suddenJoltJiggleDuration)
    {
        elapsed += Time.deltaTime;
        float damped = Mathf.Exp(-suddenJoltJiggleDamping * elapsed)
                     * Mathf.Sin(suddenJoltJiggleFrequency * elapsed);

        float fadeOut = 1f - Mathf.Clamp01((elapsed - suddenJoltJiggleDuration * 0.8f) / (suddenJoltJiggleDuration * 0.2f));

        currentSuddenJoltOffset = jiggleAxis * damped * fadeOut;
        yield return null;
    }

    currentSuddenJoltOffset = Vector3.zero;
    suddenJoltCoroutine = null;
}

    private void UpdateSuddenJoltPlayback()
    {
        if (!isSuddenJolting && !isJiggling)
        {
            currentSuddenJoltOffset = Vector3.zero;
            return;
        }

        float progress = (Time.time - suddenJoltStartTime) / Mathf.Max(0.0001f, currentSuddenJoltDuration);

        if (progress >= 1f)
        {
            isSuddenJolting = false;
            currentSuddenJoltOffset = Vector3.zero;
            return;
        }

        currentSuddenJoltOffset = EvaluatePingPongJolt(
            suddenJoltTargetOffset,
            progress,
            suddenJoltCurve
        );
    }

    private IEnumerator ClearSuddenJoltAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);

        isSuddenJolting = false;
        currentSuddenJoltOffset = Vector3.zero;
        suddenJoltCoroutine = null;
    }

    private Vector3 EvaluatePingPongJolt(Vector3 targetOffset, float progress, AnimationCurve curve)
    {
        progress = Mathf.Clamp01(progress);

        if (progress <= 0.5f)
        {
            float outward01 = progress / 0.5f;
            float curved = curve.Evaluate(outward01);
            return Vector3.LerpUnclamped(Vector3.zero, targetOffset, curved);
        }
        else
        {
            float return01 = (progress - 0.5f) / 0.5f;
            float curved = curve.Evaluate(return01);
            return Vector3.LerpUnclamped(targetOffset, Vector3.zero, curved);
        }
    }
private void ApplyMotion()
{
    Vector3 finalPos = originalPosition + currentAmbientJoltOffset + currentSuddenJoltOffset;
    transform.localPosition = finalPos;
   
}
    public void SetSwayEnabled(bool enabled)
    {
        enableSway = enabled;

        if (!enabled)
        {
            ResetRotation();
        }
    }

    public void SetAmbientJoltsEnabled(bool enabled)
    {
        enableAmbientJolts = enabled;

        if (!enabled)
        {
            StopAmbientJolt();
            transform.localPosition = originalPosition + currentSuddenJoltOffset;
        }
        else
        {
            ScheduleNextAmbientJolt();
        }
    }

    public void TriggerManualAmbientJolt()
    {
        if (enableAmbientJolts && !isAmbientJolting)
        {
            StartAmbientJolt();
        }
    }

    public float GetCurrentSpeedPercentage()
    {
        return currentSpeedPercentage;
    }

    public void ResetMotion()
    {
        StopAmbientJolt();

        if (suddenJoltCoroutine != null)
        {
            StopCoroutine(suddenJoltCoroutine);
            suddenJoltCoroutine = null;
        }

        isSuddenJolting = false;
        currentSuddenJoltOffset = Vector3.zero;

        transform.localPosition = originalPosition;
        transform.localRotation = Quaternion.Euler(originalRotation);

        ScheduleNextAmbientJolt();
    }
}