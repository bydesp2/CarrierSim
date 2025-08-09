using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Data")]
    public VehicleData vehicleData;          // Mevcut veri sınıfın (ScriptableObject değil)
    public Transform playerPlace;

    [Header("State")]
    [Tooltip("Current fuel level (0–1)")]
    public float currentFuel;
    [Tooltip("Current vehicle condition (0–1)")]
    public float condition;

    private Rigidbody rb;
    public bool isPlayerIn = false;

    // --- UI debug (opsiyonel) ---
    public Text throttleTx, steerTx;

    // --- Input cache ---
    float cachedThrottle; // -1..1
    float cachedSteer;    // -1..1
    bool cachedBrake;    // İstersen boşluk freni ekleyebilirsin

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);

        if (vehicleData != null)
        {
            currentFuel = vehicleData.fuel;
            condition = vehicleData.condition;
        }

        // Arcade için önerilen ama zorunlu olmayan ayarlar:
        // rb.interpolation = RigidbodyInterpolation.Interpolate;
        // rb.constraints   = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // --- Input sadece burada okunur (frame güvenli) ---
    void Update()
    {
        if (!isPlayerIn) return;

        cachedSteer = Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);
        cachedThrottle = Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1f, 1f);
        cachedBrake = Input.GetKey(KeyCode.Space);

        if (throttleTx) throttleTx.text = cachedThrottle.ToString("F3");
        if (steerTx) steerTx.text = cachedSteer.ToString("F3");
    }

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

        // 1) Hız ve hız limiti (lokal uzayda)
        Vector3 velWorld = rb.velocity;
        Vector3 velLocal = transform.InverseTransformDirection(velWorld);
        float fwdSpeed = velLocal.z; // m/s (+ ileri, - geri)

        float maxF = Mathf.Max(0.1f, vehicleData.maxSpeed);
        float maxR = Mathf.Max(0.1f, vehicleData.maxReverseSpeed);

        // Limit uygula (lokalde)
        if (velLocal.z > maxF) velLocal.z = maxF;
        if (velLocal.z < -maxR) velLocal.z = -maxR;

        // Dünyaya geri yaz ve telemetri güncelle
        rb.velocity = transform.TransformDirection(velLocal);
        fwdSpeed = velLocal.z;

        // 2) Direksiyon — hıza bağlı yumuşama + max açısal hız clamp
        // Hız arttıkça direksiyon etkisini biraz azalt (0..1)
        float speedRatio = Mathf.Clamp01(Mathf.Abs(fwdSpeed) / maxF);
        // 1.0 (yavaşken) → 0.4 (maks hıza yakınken) gibi bir ölçek:
        float steerScale = Mathf.Lerp(1.0f, 0.4f, speedRatio);

        // Geri giderken yön hissi için küçük bir tersleme:
        float reverseSign = fwdSpeed >= 0f ? 1f : -1f;

        // Hedef açısal hız (rad/sn), Y ekseni
        float targetYaw = steer * reverseSign * vehicleData.maxAngularVelocity * steerScale;

        // Mevcut açısal hıza yumuşak yaklaş (kritik: ani spin yok)
        Vector3 av = rb.angularVelocity;
        float blend = 0.15f + 0.85f * (1f - speedRatio); // yavaşken daha hızlı yaklaş
        av.y = Mathf.Lerp(av.y, targetYaw, blend);

        // Güvenlik: clamp
        av.y = Mathf.Clamp(av.y, -vehicleData.maxAngularVelocity, vehicleData.maxAngularVelocity);
        rb.angularVelocity = av;

        // 3) İtiş / Fren — P‑controller ile hedef hız takibi
        float desiredSpeed =
            (throttle >= 0f ? vehicleData.maxSpeed * throttle
                            : vehicleData.maxReverseSpeed * throttle);

        float speedError = desiredSpeed - fwdSpeed;

        // Space frenini istersen aktif et: pozitif/negatif hıza karşı koy
        if (brake)
        {
            // basit fren: hızın tersine bir hedef
            speedError = -fwdSpeed;
        }

        // Kuvveti hesapla (Acc/F = m*a değil; ForceMode.Acceleration ile direkt a veriyoruz)
        float accelGain = Mathf.Max(0.01f, vehicleData.accelerationForce);
        float accelCmd = speedError * accelGain;

        rb.AddForce(transform.forward * accelCmd, ForceMode.Acceleration);

        // 4) Yakıt & kondisyon tüket — yalnızca “etkili gaz” varken
        if (Mathf.Abs(throttle) > 0.05f && currentFuel > 0f && condition > 0f)
        {
            ConsumeFuelAndCondition(Mathf.Abs(throttle));
        }
    }

    private void ConsumeFuelAndCondition(float throttleAmount)
    {
        // NOT: fuelPerSecond ve conditionDecayRate VehicleData içinde
        float fuelUse = vehicleData.fuelPerSecond * throttleAmount * Time.fixedDeltaTime;
        currentFuel = Mathf.Max(currentFuel - fuelUse, 0f);

        float condUse = vehicleData.conditionDecayRate * throttleAmount * Time.fixedDeltaTime;
        condition = Mathf.Max(condition - condUse, 0f);
    }

    // ------------------------------------------------------------
    // ------------------- DİĞER (DEĞİŞMEDİ) ----------------------
    // ------------------------------------------------------------

    public void PlayerGetInOut(bool getIn)
    {
        isPlayerIn = getIn;
    }

    public void RefillFuel(float refillAmount)
    {
        currentFuel = Mathf.Clamp01(currentFuel + refillAmount);
    }

    public VehicleData GetVehicleSaveData()
    {
        // Mevcut sisteminle uyumlu: runtime state’i data’ya geri yaz
        vehicleData.fuel = currentFuel;
        vehicleData.condition = condition;

        // Konum kaydı (yüklemede zıplama olmasın diye hafif yukarı)
        vehicleData.lastPosition = transform.position + Vector3.up * 0.1f;

        return vehicleData;
    }

    public void LoadVehicleFromSaveData(VehicleData loadData)
    {
        vehicleData = loadData;

        currentFuel = loadData.fuel;
        condition = loadData.condition;

        transform.position = loadData.lastPosition;

        // İstersen mevcut hızları sıfırlayalım ki yüklemede “sıçrama” olmasın:
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
