using UnityEngine;

[System.Serializable]
public class VehicleData
{
    [Header("Drive Type & Layout")]
    [Tooltip("Four-wheel vehicle (car) if true, two-wheel (bike) if false")]
    public bool fourWheel = true;
    [Tooltip("Front-Wheel Drive if true, Rear-Wheel Drive if false (only meaningful when fourWheel=true)")]
    public bool isFWD = true;

    [Header("Movement (legacy, still used)")]
    public float maxSpeed = 40f;         // m/s (forward)
    public float maxReverseSpeed = 12f;  // m/s (reverse)
    public float accelerationForce = 10f;// m/s^2, sadece hedef hýza yaklaþým hýzýný þekillendirmede kullanýlýyor

    [Header("Steering")]
    [Tooltip("Max steer angle at very low speeds (deg)")] 
    public float maxSteerDegLowSpeed = 35f;
    [Tooltip("Max steer angle at high speeds (deg)")]
    public float maxSteerDegHighSpeed = 8f;
    [Tooltip("How fast steering reaches target (deg/sec)")]
    public float steerSpeedDegPerSec = 180f;

    [Header("Geometry")]
    [Tooltip("Distance between front and rear axles (m)")]
    public float wheelbase = 2.6f;
    [Tooltip("CG to front axle distance (m). Rear distance = wheelbase - this")]
    public float cgToFrontAxle = 1.2f;
    [Tooltip("Center of mass height from ground (m)")]
    public float cgHeight = 0.35f;

    [Header("Tyre / Grip")]
    [Tooltip("Front cornering stiffness (N/rad)")]
    public float cornerStiffnessFront = 80000f;
    [Tooltip("Rear cornering stiffness (N/rad)")]
    public float cornerStiffnessRear = 90000f;
    [Tooltip("Peak friction coefficient (mu). Dry asphalt ~1.0 - 1.2")]
    public float maxGripMu = 1.05f;
    [Tooltip("Slip angle (deg) around which lateral force starts saturating (Pacejka-lite)")]
    public float slipAngleLimitDeg = 10f;
    [Tooltip("Lateral grip multiplier when handbrake is on (0..1). 0.5 = easier drift")]
    public float handbrakeGripMul = 0.5f;

    [Header("Longitudinal")]
    [Tooltip("Engine drive force capacity at wheels (N)")]
    public float engineForce = 8000f;
    [Tooltip("Total braking force capacity (N)")]
    public float brakeForce = 12000f;
    [Tooltip("Handbrake additional rear brake capacity (N)")]
    public float handbrakeForce = 8000f;
    [Tooltip("Below this speed (m/s) reverse can engage")]
    public float reverseEngageSpeed = 0.5f;

    [Header("Aero & Rolling")]
    [Tooltip("Aerodynamic drag coefficient lumped (N / (m/s)^2)")]
    public float airDrag = 0.35f;
    [Tooltip("Rolling resistance coefficient (N / (m/s))")]
    public float rollingResistance = 12f;
    [Tooltip("Downforce coefficient (N / (m/s)^2)")]
    public float downforce = 0.0f;

    [Header("Fuel & Condition")]
    public float fuelPerSecond = 0.01f;
    public float fuel = 1f;
    public float condition = 1f;
    public float conditionDecayRate = 0.01f;

    [Header("Save State")]
    public Vector3 lastPosition;
}
