using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Data")]
    public VehicleData vehicleData;
    public Transform playerPlace;

    [Header("State (0–1)")]
    public float currentFuel = 1f;
    public float condition = 1f;
    public bool isPlayerIn = false;

    // Dahili girişler
    float throttle;   // -1..1 (W/S)
    float steer;      // -1..1 (A/D)
    bool handbrake;   // Space
    float brakeInput; // 0..1  (otomatik fren mantığı)

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Küp devrilmesin:
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (vehicleData != null)
        {
            currentFuel = vehicleData.fuel;
            condition = vehicleData.condition;
            rb.maxAngularVelocity = Mathf.Max(4f, vehicleData.maxAngularVelocity);
        }
    }

    void Update()
    {
        if (!isPlayerIn) return;

        // WASD + Space
        float rawV = Input.GetAxisRaw("Vertical");   // W/S
        float rawH = Input.GetAxisRaw("Horizontal"); // A/D
        bool space = Input.GetKey(KeyCode.Space);

        ApplySimpleInput(rawV, rawH, space);
    }

    void FixedUpdate()
    {
        if (vehicleData == null) return;

        float dt = Time.fixedDeltaTime;
        Vector3 fwd = transform.forward;

        // Hız bileşenleri
        float vForward = Vector3.Dot(rb.velocity, fwd);
        Vector3 vFwdVec = fwd * vForward;
        Vector3 vLatVec = rb.velocity - vFwdVec;

        // 1) Gaz / Fren / Geri
        float drive = 0f;
        float brake = 0f;

        if (throttle < 0f && Mathf.Abs(vForward) > 1f) // önce fren
            brake = Mathf.Clamp01(-throttle);
        else
            drive = throttle; // ileri (+) veya durmaya yakınken geri (-)

        // İtiş
        if (drive != 0f && currentFuel > 0f && condition > 0.05f)
        {
            bool allow =
                (drive > 0f && vForward < vehicleData.maxSpeed) ||
                (drive < 0f && vForward > -vehicleData.maxReverseSpeed);

            if (allow)
            {
                float forceN = drive * vehicleData.accelerationForce * Mathf.Clamp01(condition);
                rb.AddForce(fwd * forceN, ForceMode.Force);
            }
        }

        // Fren (otomatik veya Space ile)
        if (brakeInput > 0f || brake > 0f)
        {
            float b = Mathf.Max(brakeInput, brake);
            float forceN = b * vehicleData.brakeForce;
            rb.AddForce(-Mathf.Sign(vForward) * fwd * forceN, ForceMode.Force);
        }

        if (handbrake)
            rb.AddForce(-Mathf.Sign(vForward) * fwd * vehicleData.handbrakeForce, ForceMode.Force);

        // 2) Direksiyon — Y eksenine tork
        if (Mathf.Abs(steer) > 0.001f)
        {
            float reverseSign = (vForward >= 0f) ? 1f : -1f;
            float speedRatio = Mathf.Clamp01(Mathf.Abs(vForward) / Mathf.Max(0.01f, vehicleData.maxSpeed));
            float steerScale = Mathf.Lerp(vehicleData.steerSlowScale, vehicleData.steerFastScale, speedRatio);

            if (Mathf.Abs(vForward) > vehicleData.minSteerSpeed)
            {
                float yawTorque = steer * reverseSign * steerScale * vehicleData.steeringTorque;
                rb.AddTorque(Vector3.up * yawTorque, ForceMode.Force);
            }
        }

        // 3) Basit lateral grip — yan kaymayı bastır
        if (vehicleData.lateralGrip > 0f)
        {
            Vector3 cancel = -vLatVec * vehicleData.lateralGrip * vehicleData.steerAssist;
            rb.AddForce(cancel, ForceMode.Acceleration);
        }

        // 4) Doğal yavaşlama (gaz yokken)
        if (Mathf.Abs(throttle) < 0.001f && !handbrake && brakeInput <= 0f)
        {
            Vector3 coast = -vFwdVec * vehicleData.naturalDrag;
            rb.AddForce(coast, ForceMode.Acceleration);
        }

        // 5) Hız sınırı (yalnızca ileri-geri bileşenine uygula)
        LimitLongitudinalSpeed(vehicleData.maxSpeed, vehicleData.maxReverseSpeed);

        // 6) Yakıt & kondisyon
        float load = Mathf.Abs(throttle) + (brakeInput * 0.5f) + (handbrake ? 0.5f : 0f);
        ConsumeFuelAndCondition(load * dt);
    }

    // --- Basit Input setleyici ---
    void ApplySimpleInput(float vert, float horiz, bool space)
    {
        steer = Mathf.Clamp(horiz, -1f, 1f);
        throttle = Mathf.Clamp(vert, -1f, 1f);
        handbrake = space;
        brakeInput = 0f; // ekstra fren tuşu yok; S tuşu ile auto-fren
    }

    // Harici akış için (istersen PlayerController çağırır)
    public void FixedUpdateRemoteControl(Vector3 moveDirection)
    {
        if (!isPlayerIn) return;
        ApplySimpleInput(moveDirection.z, moveDirection.x, space: false);
    }

    // ----------------- SAVE / LOAD — İMZALAR KORUNDU -----------------

    public void PlayerGetInOut(bool enter, Transform playerRoot = null)
    {
        isPlayerIn = enter;
        if (enter)
        {
            if (playerRoot && playerPlace)
            {
                playerRoot.SetParent(playerPlace, false);
                playerRoot.localPosition = Vector3.zero;
                playerRoot.localRotation = Quaternion.identity;
            }
            throttle = steer = 0f; handbrake = false; brakeInput = 0f;
        }
        else
        {
            if (playerRoot)
            {
                playerRoot.SetParent(null, true);
                playerRoot.position = transform.position + transform.right * -0.8f;
            }
            throttle = steer = 0f; handbrake = false; brakeInput = 0f;
        }
    }

    public VehicleData GetVehicleSaveData()
    {
        // Projendeki beklentiyle uyumlu: VehicleData döner
        vehicleData.fuel = currentFuel;
        vehicleData.condition = condition;
        vehicleData.lastPosition = transform.position + Vector3.up * 0.1f; // yere gömülmeyi önle
        return vehicleData;
    }

    public void LoadVehicleFromSaveData(VehicleData loadData)
    {
        vehicleData = loadData;
        currentFuel = Mathf.Clamp01(loadData.fuel);
        condition = Mathf.Clamp01(loadData.condition);

        transform.position = loadData.lastPosition;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Güvenli taraf: tekrar açılsa da dönme serbest kalsın
        rb.maxAngularVelocity = Mathf.Max(4f, vehicleData.maxAngularVelocity);
    }

    public void ConsumeFuelAndCondition(float useAmount /* ~0..1 per fixed frame */)
    {
        if (currentFuel > 0f)
            currentFuel = Mathf.Max(0f, currentFuel - vehicleData.fuelPerSecond * useAmount * 60f);
        condition = Mathf.Max(0f, condition - vehicleData.conditionDecay * useAmount * 60f);
    }

    // Yardımcı
    void LimitLongitudinalSpeed(float maxF, float maxR)
    {
        Vector3 fwd = transform.forward;
        float vF = Vector3.Dot(rb.velocity, fwd);
        float clamped = Mathf.Clamp(vF, -maxR, maxF);
        float diff = clamped - vF;
        if (Mathf.Abs(diff) > 0.0001f)
            rb.velocity += fwd * diff;
    }
}
