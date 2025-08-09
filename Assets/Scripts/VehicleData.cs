using UnityEngine;

[System.Serializable]
public class VehicleData
{
    [Header("Speed (m/s)")]
    public float maxSpeed = 22f;          // ~79 km/h
    public float maxReverseSpeed = 8f;    // ~29 km/h

    [Header("Forces (N)")]
    public float accelerationForce = 8000f;
    public float brakeForce = 12000f;
    public float handbrakeForce = 18000f;

    [Header("Steering")]
    public float steeringTorque = 1500f;  // Y eksenine tork ölçeði
    public float maxAngularVelocity = 2.5f; // Rigidbody için üst sýnýr (rad/s)
    public float minSteerSpeed = 1.5f;   // direksiyon için minimum hýz (m/s)
    public float steerSlowScale = 1.0f;  // düþük hýzda direksiyon etkisi
    public float steerFastScale = 0.2f;  // yüksek hýzda direksiyon etkisi

    [Header("Stability")]
    [Range(0f, 1f)] public float lateralGrip = 0.85f; // yan kayma bastýrma
    public float steerAssist = 2.5f;                  // dengeye yardým
    public float naturalDrag = 2.5f;                  // coast & yanal sönüm

    [Header("Fuel & Condition (0–1)")]
    public float fuel = 1f;
    public float condition = 1f;
    public float fuelPerSecond = 0.0025f;
    public float conditionDecay = 0.0003f;

    [Header("Save")]
    public Vector3 lastPosition;
}
