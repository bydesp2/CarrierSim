using UnityEngine;
using UnityEngine.UI;
using System; // System.Math.Tanh

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Data")]
    public VehicleData vehicleData;
    public Transform playerPlace;

    [Header("State")]
    [Tooltip("Current fuel level (0–1)")] public float currentFuel = 1f;
    [Tooltip("Current vehicle condition (0–1)")] public float condition = 1f;

    [Header("Debug (optional)")]
    public Text throttleTx, steerTx;

    public bool isPlayerIn = false;

    // --- internal ---
    Rigidbody rb;

    // steering state (deg)
    float steerAngleDeg;     // current smoothed steer
    float targetSteerDeg;    // desired steer

    // cached for weight transfer
    float prevForwardSpeed;  // m/s (local)
    float forwardSpeed;      // m/s (local)

    // input cache (Update -> FixedUpdate)
    float _cachedThrottle;
    bool _cachedHandbrake;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (rb.angularDrag < 0.05f) rb.angularDrag = 0.05f;

        // Emniyet: yanlış kısıtları uyar
        if ((rb.constraints & RigidbodyConstraints.FreezePositionZ) != 0)
            Debug.LogWarning("[VehicleController] Freeze Position Z ON -> araç ileri/geri gidemez. Lütfen kapat.");
        if ((rb.constraints & RigidbodyConstraints.FreezePositionX) != 0)
            Debug.LogWarning("[VehicleController] Freeze Position X ON -> yan dinamikler çalışmaz. Lütfen kapat.");
        if ((rb.constraints & RigidbodyConstraints.FreezeRotationY) != 0)
            Debug.LogWarning("[VehicleController] Freeze Rotation Y ON -> dönemeyecek. Lütfen kapat.");
    }

    void Start()
    {
        if (vehicleData != null)
        {
            currentFuel = vehicleData.fuel;
            condition = vehicleData.condition;
        }
        prevForwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        forwardSpeed = prevForwardSpeed;
    }

    void Update()
    {
        if (!isPlayerIn) return;

        float steerInput = Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);
        float throttleInput = Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1f, 1f);
        bool handbrake = Input.GetKey(KeyCode.Space);

        if (throttleTx) throttleTx.text = throttleInput.ToString("F3");
        if (steerTx) steerTx.text = steerInput.ToString("F3");

        // hız tabanlı maksimum direksiyon (deg)
        float maxF = Mathf.Max(0.1f, vehicleData.maxSpeed);
        float speedAbs = Mathf.Abs(Vector3.Dot(rb.velocity, transform.forward));
        float t = Mathf.Clamp01(speedAbs / maxF);
        float maxSteerNow = Mathf.Lerp(vehicleData.maxSteerDegLowSpeed, vehicleData.maxSteerDegHighSpeed, t);

        targetSteerDeg = steerInput * maxSteerNow;

        // steer yumuşatma
        float step = vehicleData.steerSpeedDegPerSec * Time.deltaTime;
        steerAngleDeg = Mathf.MoveTowards(steerAngleDeg, targetSteerDeg, step);

        // Yakıt & kondisyon: yalnızca pozitif gazda tüket
        if (throttleInput > 0.05f && currentFuel > 0f && condition > 0f)
            ConsumeFuelAndCondition(throttleInput);

        // cache -> FixedUpdate
        _cachedThrottle = throttleInput;
        _cachedHandbrake = handbrake;
    }

    void FixedUpdate()
    {
        if (!isPlayerIn || vehicleData == null) return;

        // world -> local hızlar
        Vector3 vLocal = transform.InverseTransformDirection(rb.velocity);
        // ileri (+), geri (−). Bölümde 0 patlamasın diye ayrı mutlak kullanalım
        float vxAbs = Mathf.Max(0.01f, Mathf.Abs(vLocal.z));
        float vy = vLocal.x;                       // yan hız (local X)
        float r = rb.angularVelocity.y;           // yaw rate (rad/s)

        // geometri
        float wb = Mathf.Max(0.1f, vehicleData.wheelbase);
        float Lf = Mathf.Clamp(vehicleData.cgToFrontAxle, 0.01f, wb - 0.01f);
        float Lr = wb - Lf;
        float h = Mathf.Max(0.05f, vehicleData.cgHeight);

        // direksiyon
        float steerRad = steerAngleDeg * Mathf.Deg2Rad;

        // slip açıları (rad)
        // atan2(vy + L * r, |vx|) − delta
        float alpha_f = Mathf.Atan2(vy + Lf * r, vxAbs) - steerRad;
        float alpha_r = Mathf.Atan2(vy - Lr * r, vxAbs);

        // cornering stiffness
        float Cf = vehicleData.cornerStiffnessFront;
        float Cr = vehicleData.cornerStiffnessRear;

        // handbrake: arka lateral tutuş düşsün (drift tetikleyici)
        float handMul = _cachedHandbrake ? Mathf.Clamp01(vehicleData.handbrakeGripMul) : 1f;
        Cr *= handMul;

        // slip saturasyon (Pacejka-lite) -> tanh ile yumuşak doyum
        float satRad = Mathf.Max(1f * Mathf.Deg2Rad, vehicleData.slipAngleLimitDeg * Mathf.Deg2Rad);

        float Fy_f_lin = -Cf * alpha_f;
        float Fy_r_lin = -Cr * alpha_r;

        float Fy_f = Tanh(alpha_f / satRad) * Mathf.Abs(Fy_f_lin);
        Fy_f *= -Mathf.Sign(alpha_f);

        float Fy_r = Tanh(alpha_r / satRad) * Mathf.Abs(Fy_r_lin);
        Fy_r *= -Mathf.Sign(alpha_r);

        // normal yükler (weight transfer)
        float m = rb.mass;
        float g = Physics.gravity.magnitude;

        // uzunlamasına ivme tahmini
        float vForwardNow = vLocal.z;
        float ax_est = (vForwardNow - prevForwardSpeed) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        prevForwardSpeed = vForwardNow;

        float Nf_static = m * g * (Lr / wb);
        float Nr_static = m * g * (Lf / wb);
        float dF = m * ax_est * h / wb;           // +accel -> arka yük artar
        float Nf = Mathf.Max(0f, Nf_static - dF);
        float Nr = Mathf.Max(0f, Nr_static + dF);

        // friction circle limiti
        float mu = Mathf.Max(0.1f, vehicleData.maxGripMu);

        // Longitudinal forces (drive / brake)
        float throttle = _cachedThrottle;
        float Fx_f = 0f, Fx_r = 0f;

        bool wantsReverse = (throttle < -0.05f);
        bool canReverse = Mathf.Abs(vForwardNow) < vehicleData.reverseEngageSpeed;

        // sürüş kuvveti
        float driveSign = 1f; // ileri
        if (wantsReverse && canReverse) driveSign = -1f;

        float driveForce = Mathf.Max(0f, vehicleData.engineForce) * Mathf.Max(0f, throttle) * driveSign;

        if (vehicleData.fourWheel)
        {
            if (vehicleData.isFWD) Fx_f += driveForce; else Fx_r += driveForce;
        }
        else
        {
            // 2 teker: default arka tahrik
            Fx_r += driveForce;
        }

        // fren (geri tuşu > 0 hızda fren, el freni arkaya ilave)
        float brakeInput = 0f;
        if (throttle < -0.05f && !canReverse) brakeInput = -throttle;

        float Fb_total = vehicleData.brakeForce * Mathf.Clamp01(brakeInput);
        float Fb_hand = vehicleData.handbrakeForce * (_cachedHandbrake ? 1f : 0f);

        float Fb_f = Fb_total * 0.65f;
        float Fb_r = Fb_total * 0.35f + Fb_hand;

        Fx_f -= Fb_f;
        Fx_r -= Fb_r;

        // friction circle: (Fx^2 + Fy^2) <= (mu*N)^2
        Vector2 Ff = FrictionCircleClamp(Fx_f, Fy_f, mu * Nf);
        Vector2 Fr = FrictionCircleClamp(Fx_r, Fy_r, mu * Nr);
        Fx_f = Ff.x; Fy_f = Ff.y;
        Fx_r = Fr.x; Fy_r = Fr.y;

        // Aero & rolling (world space)
        Vector3 vWorld = rb.velocity;
        Vector3 vDir = vWorld.sqrMagnitude > 0.0001f ? vWorld.normalized : Vector3.zero;
        Vector3 F_aero = -vehicleData.airDrag * vWorld.sqrMagnitude * vDir;
        Vector3 F_roll = -vehicleData.rollingResistance * vWorld.magnitude * vDir;
        Vector3 F_down = -Vector3.up * (vehicleData.downforce * vWorld.sqrMagnitude);

        // Force application points (world)
        Vector3 frontPos = transform.position + transform.forward * Lf;
        Vector3 rearPos = transform.position - transform.forward * Lr;

        // Ön teker yönleri
        Quaternion steerRot = Quaternion.Euler(0f, steerAngleDeg, 0f);
        Vector3 frontDir = steerRot * transform.forward; // long.
        Vector3 frontRight = steerRot * transform.right;   // lateral (DÜZELTİLDİ)

        // Kuvvetleri world eksenine çevir
        Vector3 F_front = frontDir * Fx_f + frontRight * Fy_f;
        Vector3 F_rear = transform.forward * Fx_r + transform.right * Fy_r;

        // Uygula
        rb.AddForceAtPosition(F_front, frontPos, ForceMode.Force);
        rb.AddForceAtPosition(F_rear, rearPos, ForceMode.Force);

        // Aero & rolling & downforce
        rb.AddForce(F_aero + F_roll + F_down, ForceMode.Force);

        // MaxSpeed clamp (oyun tasarımı için güvenlik)
        LimitForwardSpeed(vehicleData.maxSpeed, vehicleData.maxReverseSpeed);

        // telemetri
        forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
    }

    // --- helpers ---

    static Vector2 FrictionCircleClamp(float Fx, float Fy, float limit)
    {
        float mag = Mathf.Sqrt(Fx * Fx + Fy * Fy);
        if (mag <= limit || mag < 0.0001f) return new Vector2(Fx, Fy);
        float s = limit / mag;
        return new Vector2(Fx * s, Fy * s);
    }

    static float Tanh(float x) => (float)System.Math.Tanh(x);

    void LimitForwardSpeed(float maxF, float maxR)
    {
        Vector3 lv = transform.InverseTransformDirection(rb.velocity);
        if (lv.z > maxF) lv.z = maxF;
        if (lv.z < -maxR) lv.z = -maxR;
        rb.velocity = transform.TransformDirection(lv);
    }

    void ConsumeFuelAndCondition(float throttleAmount)
    {
        float dt = Time.deltaTime;
        currentFuel = Mathf.Max(0f, currentFuel - vehicleData.fuelPerSecond * throttleAmount * dt);
        condition = Mathf.Max(0f, condition - vehicleData.conditionDecayRate * throttleAmount * dt);
    }

    // --- Save/Load & Player enter/exit ---

    public void PlayerGetInOut(bool getIn)
    {
        isPlayerIn = getIn;

        if (!getIn)
        {
            _cachedThrottle = 0f;
            _cachedHandbrake = false;
            targetSteerDeg = 0f;
        }
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

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        prevForwardSpeed = 0f;
    }
}
