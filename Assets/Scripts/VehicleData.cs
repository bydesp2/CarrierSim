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
    public float steeringTorque = 1500f;  // Y eksenine tork �l�e�i
    public float maxAngularVelocity = 2.5f; // Rigidbody i�in �st s�n�r (rad/s)
    public float minSteerSpeed = 1.5f;   // direksiyon i�in minimum h�z (m/s)
    public float steerSlowScale = 1.0f;  // d���k h�zda direksiyon etkisi
    public float steerFastScale = 0.2f;  // y�ksek h�zda direksiyon etkisi

    [Header("Stability")]
    [Range(0f, 1f)] public float lateralGrip = 0.85f; // yan kayma bast�rma
    public float steerAssist = 2.5f;                  // dengeye yard�m
    public float naturalDrag = 2.5f;                  // coast & yanal s�n�m

    [Header("Fuel & Condition (0�1)")]
    public float fuel = 1f;
    public float condition = 1f;
    public float fuelPerSecond = 0.0025f;
    public float conditionDecay = 0.0003f;

    [Header("Save")]
    public Vector3 lastPosition;
}
