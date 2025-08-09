using UnityEngine;

[System.Serializable]
public class VehicleData
{
    [Header("Movement")]
    [Tooltip("Maximum forward speed in units/sec")]
    public float maxSpeed;
    [Tooltip("Maximum reverse speed in units/sec")]
    public float maxReverseSpeed;
    [Tooltip("Base acceleration force applied for throttle")]
    public float accelerationForce;

    [Header("Steering Settings")]
    [Tooltip("Torque applied for steering")]
    public float steeringTorque = 300f;
    public float maxAngularVelocity = 2f;  // rad/s cinsinden, örneðin ~114°/s = 2 rad/s

    [Header("Steering Extra")]
    public float minSteerSpeed = 0.5f;     // çok yavaþken direksiyon limiti
    public float steerSlowScale = 1.0f;    // yavaþ hýz direksiyon ölçeði
    public float steerFastScale = 0.4f;    // yüksek hýz direksiyon ölçeði

    [Header("Drag")]
    public float naturalDrag = 0.05f;

    [Header("Fuel & Condition")]
    [Tooltip("Fuel consumed per second at full throttle")]
    public float fuelPerSecond;
    [Tooltip("Initial and current fuel level (0–1)")]
    public float fuel;
    [Tooltip("Initial and current vehicle condition (0–1)")]
    public float condition;
    [Tooltip("Condition decay rate per throttle unit per second")]
    public float conditionDecayRate = 0.001f;

    [Header("Save State")]
    [Tooltip("Last saved position for respawn or persistence")]
    public Vector3 lastPosition;
}
