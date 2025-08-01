using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rockingController : MonoBehaviour
{

     


    
    [Header("Speed-Based Swaying Motion")]
    [SerializeField] private float baseSwayAmount = 2f;      // Max degrees of Z-axis rotation at full speed
    [SerializeField] private float minSwayAmount = 0.2f;     // Min degrees when stationary/slow
    [SerializeField] private float baseSwaySpeed = 0.8f;     // Speed of swaying motion at full speed
    [SerializeField] private float minSwaySpeed = 0.1f;      // Min sway speed when stationary/slow
    [SerializeField] private bool enableSway = true;
    
    [Header("Speed-Based Random Jolts")]
    [SerializeField] private bool enableJolts = true;
    [SerializeField] private float baseJoltMinInterval = 1f;     // Minimum time between jolts at full speed
    [SerializeField] private float baseJoltMaxInterval = 4f;     // Maximum time between jolts at full speed
    [SerializeField] private float slowJoltMinInterval = 8f;     // Minimum time between jolts when slow
    [SerializeField] private float slowJoltMaxInterval = 20f;    // Maximum time between jolts when slow
    
    [Header("Speed-Based Jolt Strength")]
    [SerializeField] private Vector2 baseForwardBackwardRange = new Vector2(-0.4f, 0.3f);  // X-axis movement at full speed
    [SerializeField] private Vector2 baseUpDownRange = new Vector2(-0.2f, 0.5f);           // Y-axis movement at full speed
    [SerializeField] private Vector2 baseLeftRightRange = new Vector2(-0.3f, 0.3f);        // Z-axis movement at full speed
    [SerializeField] private float minJoltMultiplier = 0.1f;                              // Minimum jolt strength when slow
    
    [Header("Jolt Timing")]
    [SerializeField] private float joltDuration = 0.3f;      // How long each jolt lasts
    [SerializeField] private AnimationCurve joltCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public GameObject tcObj;
    public GameObject cameraObj;
    private TrainControl tc;
    private cameraControl cc;
    
    // Private variables
    private Vector3 originalPosition;
    private Vector3 originalRotation;
    private float swayOffset;
    private float nextJoltTime;
    private bool isJolting = false;
    private float joltStartTime;
    private Vector3 joltTargetOffset;
    private Vector3 currentJoltOffset;
    
    // Speed-based calculation cache
    private float currentSpeedPercentage;
    private float currentSwayAmount;
    private float currentSwaySpeed;
    
    void Start() 
    {
        cc = cameraObj.GetComponent<cameraControl>();
        tc = tcObj.GetComponent<TrainControl>();
        
        // Store original transform values
        originalPosition = transform.localPosition;
        originalRotation = transform.localEulerAngles;
        
        // Random starting phase for sway to make multiple carriages look different
        swayOffset = Random.Range(0f, Mathf.PI * 2);
        
        // Schedule first jolt
        ScheduleNextJolt();
    }
    
    void Update() 
    {
         if (cc.outAmount > 0 && tc.trainCurrentSpeed > 2) {
        // Calculate current speed percentage
        UpdateSpeedPercentage();
        
        // Handle swaying motion
        if (enableSway)
        {
            UpdateSway();
        }
        
        // Handle random jolts
        if (enableJolts)
        {
            UpdateJolts();
        }
        
        // Apply combined motion
        ApplyMotion();
         }
    }
    
    private void UpdateSpeedPercentage()
    {
        if (tc != null && tc.trainTopSpeed > 0)
        {
            currentSpeedPercentage = Mathf.Clamp01(tc.trainCurrentSpeed / tc.trainTopSpeed);
        }
        else
        {
            // Fallback if no train controller
            currentSpeedPercentage = 0.5f; // Default to 50% for testing
        }
        
        // Calculate speed-based motion values
        currentSwayAmount = Mathf.Lerp(minSwayAmount, baseSwayAmount, currentSpeedPercentage);
        currentSwaySpeed = Mathf.Lerp(minSwaySpeed, baseSwaySpeed, currentSpeedPercentage);
    }
    
    private void UpdateSway()
    {
        // Create natural swaying with multiple sine waves using current speed-based values
        float primarySway = Mathf.Sin((Time.time + swayOffset) * currentSwaySpeed) * currentSwayAmount;
        float secondarySway = Mathf.Sin((Time.time + swayOffset) * currentSwaySpeed * 1.7f) * (currentSwayAmount * 0.3f);
        
        float totalSway = primarySway + secondarySway;
        transform.localRotation = Quaternion.Euler(
            originalRotation.x, 
            originalRotation.y, 
            originalRotation.z + totalSway
        );
    }
    
    private void UpdateJolts()
    {
        // Check if it's time for a new jolt
        if (!isJolting && Time.time >= nextJoltTime)
        {
            StartJolt();
        }
        
        // Update current jolt
        if (isJolting)
        {
            float joltProgress = (Time.time - joltStartTime) / joltDuration;
            
            if (joltProgress >= 1f)
            {
                // Jolt finished
                isJolting = false;
                currentJoltOffset = Vector3.zero;
                ScheduleNextJolt();
            }
            else
            {
                // Interpolate jolt motion using curve
                float curveValue = joltCurve.Evaluate(joltProgress);
                currentJoltOffset = Vector3.Lerp(Vector3.zero, joltTargetOffset, curveValue);
                
                // Add return motion in second half
                if (joltProgress > 0.5f)
                {
                    float returnProgress = (joltProgress - 0.5f) * 2f;
                    currentJoltOffset = Vector3.Lerp(joltTargetOffset, Vector3.zero, returnProgress);
                }
            }
        }
    }
    
    private void StartJolt()
    {
        isJolting = true;
        joltStartTime = Time.time;
        
        // Calculate speed-based jolt strength
        float joltMultiplier = Mathf.Lerp(minJoltMultiplier, 1f, currentSpeedPercentage);
        
        // Generate random jolt direction and strength based on current speed
        joltTargetOffset = new Vector3(
            Random.Range(baseForwardBackwardRange.x, baseForwardBackwardRange.y) * joltMultiplier,  // Forward/Backward
            Random.Range(baseUpDownRange.x, baseUpDownRange.y) * joltMultiplier,                    // Up/Down
            Random.Range(baseLeftRightRange.x, baseLeftRightRange.y) * joltMultiplier               // Left/Right
        );
    }
    
    private void ScheduleNextJolt()
    {
        // Calculate speed-based jolt intervals
        float minInterval = Mathf.Lerp(slowJoltMinInterval, baseJoltMinInterval, currentSpeedPercentage);
        float maxInterval = Mathf.Lerp(slowJoltMaxInterval, baseJoltMaxInterval, currentSpeedPercentage);
        
        nextJoltTime = Time.time + Random.Range(minInterval, maxInterval);
    }
    
    private void ApplyMotion()
    {
        // Apply position offset from jolts
        transform.localPosition = originalPosition + currentJoltOffset;
    }
    
    // Public methods for runtime control
    public void SetSwayEnabled(bool enabled)
    {
        enableSway = enabled;
        if (!enabled)
        {
            transform.localRotation = Quaternion.Euler(originalRotation);
        }
    }
    
    public void SetJoltsEnabled(bool enabled)
    {
        enableJolts = enabled;
        if (!enabled)
        {
            isJolting = false;
            currentJoltOffset = Vector3.zero;
            transform.localPosition = originalPosition;
        }
    }
    
    public void TriggerManualJolt()
    {
        if (enableJolts && !isJolting)
        {
            StartJolt();
        }
    }
    

    
    // Debug info
    public float GetCurrentSpeedPercentage()
    {
        return currentSpeedPercentage;
    }
    
    // Reset to original state
    public void ResetMotion()
    {
        transform.localPosition = originalPosition;
        transform.localRotation = Quaternion.Euler(originalRotation);
        isJolting = false;
        currentJoltOffset = Vector3.zero;
        ScheduleNextJolt();
    }
}
      