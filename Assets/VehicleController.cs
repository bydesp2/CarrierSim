using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Data")]
    public VehicleData vehicleData;          // Mevcut veri sınıfın (SO değil)
    public Transform playerPlace;            // Sürücü koltuğu/kamera referansı

    [Header("State")]
    [Tooltip("Current fuel level (0–1)")]
    public float currentFuel;
    [Tooltip("Current vehicle condition (0–1)")]
    public float condition;

    private Rigidbody rb;
    public bool isPlayerIn = false;

    // (Opsiyonel) Debug UI
    public Text throttleTx, steerTx;

    // ---- Input cache (Update'ta dolar, FixedUpdate'ta kullanılır) ----
    float cachedThrottle; // -1..1
    float cachedSteer;    // -1..1
    bool cachedBrake;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Tavsiye edilen fizik ayarları (Inspector’dan elle yapman daha iyi):
        //  - Freeze Rotation Y: OFF (X ve Z ON olabilir)
        //  - Interpolate: Interpolate (Extrapolate yerine)
        //  - Automatic Tensor: ON (elle inertia tensor kullanma, şimdilik)
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);

        if (vehicleData != null)
        {
            currentFuel = vehicleData.fuel;
            condition = vehicleData.condition;
        }
    }

    // ---------------- Input ----------------
    void Update()
    {
        if (!isPlayerIn) return;

        cachedSteer = Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);
        cachedThrottle = Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1f, 1f);
        cachedBrake = Input.GetKey(KeyCode.Space);

        if (throttleTx) throttleTx.text = cachedThrottle.ToString("F3");
        if (steerTx) steerTx.text = cachedSteer.ToString("F3");
    }

    // --------------- Physics step ---------------
    void FixedUpdate()
    {
        if (!isPlayerIn) return;
        Drive(cachedThrottle, cachedSteer, cachedBrake);
    } 

    // ------------------------------------------------------------
    // ------------------- HAREKET ÇEKİRDEĞİ ----------------------
    // ------------------------------------------------------------
    void Drive(float throttle, float steer, bool brake)
    {
        if (vehicleData == null) return;

        // 1) Lokal hız ve hız sınırları
        Vector3 velLocal = transform.InverseTransformDirection(rb.velocity);
        float fwdSpeed = velLocal.z; // m/s (+ ileri, - geri)

        float maxF = Mathf.Max(0.1f, vehicleData.maxSpeed);
        float maxR = Mathf.Max(0.1f, vehicleData.maxReverseSpeed);

        if (velLocal.z > maxF) velLocal.z = maxF;
        if (velLocal.z < -maxR) velLocal.z = -maxR;

        rb.velocity = transform.TransformDirection(velLocal);
        fwdSpeed = velLocal.z;

        // 2) Direksiyon — hızla ölçeklenmiş yaw (MoveRotation ile)
        float speedAbs = Mathf.Abs(fwdSpeed);
        float speedRatio = Mathf.Clamp01(speedAbs / maxF);
        float steerScale = Mathf.Lerp(vehicleData.steerSlowScale, vehicleData.steerFastScale, speedRatio);
        float minSteerSpd = Mathf.Max(0f, vehicleData.minSteerSpeed);

        if (speedAbs > minSteerSpd && Mathf.Abs(steer) > 0.01f)
        {
            // İleri/geri hissi: hız yönüne göre tersleme (geri viteste doğal)
            float reverseSign = fwdSpeed >= 0f ? 1f : -1f;

            // hedef açısal hız (rad/sn), Y ekseninde
            float targetYaw = steer * reverseSign * vehicleData.maxAngularVelocity * steerScale;

            // bu kare döneceğimiz açı (deg)
            float yawDeg = Mathf.Clamp(targetYaw, -vehicleData.maxAngularVelocity, vehicleData.maxAngularVelocity)
                           * Mathf.Rad2Deg * Time.fixedDeltaTime;

            // inertia tensor’dan daha az etkilenen güvenli dönüş
            Quaternion delta = Quaternion.Euler(0f, yawDeg, 0f);
            rb.MoveRotation(rb.rotation * delta);
        }
        else
        {
            // Çok yavaşken veya direksiyon bırakıldığında yaw sönümleme (isteğe bağlı)
            Vector3 av = rb.angularVelocity;
            av.y = Mathf.MoveTowards(av.y, 0f, rb.angularDrag * Time.fixedDeltaTime);
            rb.angularVelocity = av;
        }

        // 3) İleri/Geri ivme — P-controller + doğal yavaşlama + güçlü Auto‑Hold
        float desiredSpeed = (throttle >= 0f)
            ? vehicleData.maxSpeed * throttle
            : vehicleData.maxReverseSpeed * throttle;

        float speedError = desiredSpeed - fwdSpeed;

        // Space fren: hızı 0'a çeker
        if (brake) speedError = -fwdSpeed;

        float accelGain = Mathf.Max(0.01f, vehicleData.accelerationForce);
        float accelCmd = speedError * accelGain;

        // Gaz yoksa doğal yavaşlama (current velocity'e ters)
        if (Mathf.Approximately(throttle, 0f) && !Mathf.Approximately(fwdSpeed, 0f))
            accelCmd += -fwdSpeed * Mathf.Max(0f, vehicleData.naturalDrag);

        // Auto‑Hold: gaz yok + küçük hız + fren değilse → ileri/geri bileşeni sabitle
        const float holdThreshold = 0.10f; // m/s
        if (Mathf.Approximately(throttle, 0f) && Mathf.Abs(fwdSpeed) < holdThreshold && !brake)
        {
            Vector3 v = transform.InverseTransformDirection(rb.velocity);
            v.z = 0f;
            rb.velocity = transform.TransformDirection(v);
        }
        else
        {
            rb.AddForce(transform.forward * accelCmd, ForceMode.Acceleration);
        }

        // 4) Yakıt & kondisyon — sadece gaz verilirken
        if (Mathf.Abs(throttle) > 0.05f && currentFuel > 0f && condition > 0f)
            ConsumeFuelAndCondition(Mathf.Abs(throttle));
    }

    private void ConsumeFuelAndCondition(float throttleAmount)
    {
        // vehicleData.fuelPerSecond & conditionDecayRate kullanılır
        float fuelUse = vehicleData.fuelPerSecond * throttleAmount * Time.fixedDeltaTime;
        currentFuel = Mathf.Max(currentFuel - fuelUse, 0f);

        float condUse = vehicleData.conditionDecayRate * throttleAmount * Time.fixedDeltaTime;
        condition = Mathf.Max(condition - condUse, 0f);
    }

    // ------------------------------------------------------------
    // ------------------- DİĞERLERİ (değişmedi) ------------------
    // ------------------------------------------------------------

    public void PlayerGetInOut(bool getIn) => isPlayerIn = getIn;

    public void RefillFuel(float refillAmount)
    {
        currentFuel = Mathf.Clamp01(currentFuel + refillAmount);
    }

    public VehicleData GetVehicleSaveData()
    {
        vehicleData.fuel = currentFuel;
        vehicleData.condition = condition;
        vehicleData.lastPosition = transform.position + Vector3.up * 0.1f;
        return vehicleData;
    }

    public void LoadVehicleFromSaveData(VehicleData loadData)
    {
        vehicleData = loadData;
        currentFuel = loadData.fuel;
        condition = loadData.condition;
        transform.position = loadData.lastPosition;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
