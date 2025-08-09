using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Data")]
    public VehicleData vehicleData;          // ScriptableObject or serializable data asset
    public Transform playerPlace;            // Reference to seat or camera position when driving

    [Header("State")]
    [Tooltip("Current fuel level (0–1)")]
    public float currentFuel;
    [Tooltip("Current vehicle condition (0–1)")]
    public float condition;

    private Rigidbody rb;
    public bool isPlayerIn = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);

        if (vehicleData != null)
        {
            currentFuel = vehicleData.fuel;
            condition = vehicleData.condition;
        }
    }

    private float debugTimer = 0f;
    void Update()
    {
        if (isPlayerIn)
            UpdateRemoteControl();
        /*// --- Buradan itibaren eklenen debug kodu ---
        debugTimer += Time.fixedDeltaTime;
        if (debugTimer >= 1f)
        {
            float steer = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
            float torque = steer * vehicleData.steeringTorque;
            Debug.Log($"[1s Debug] Torque: {torque:F2}, Horizontal Input: {steer:F2}");
            debugTimer = 0f;
        }*/
    }

    public void PlayerGetInOut(bool getIn)
    { 
        isPlayerIn = getIn;
    }
    public Text throttleTx, steerTx;

    private void UpdateRemoteControl()
    {
        // 1) Read input
        float steer = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        float throttle = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        /*
        if(throttle > 0f)
        {
            throttle = 1f;
        }
        else if(throttle < 0f)
        {
            throttle = -1f;
        }*/

        throttleTx.text = throttle.ToString("F3");
        steerTx.text = steer.ToString("F3");

        // 2) Compute speeds
        Vector3 vel = rb.velocity;
        Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);
        float flatSpeed = flatVel.magnitude;

        // --- NEW STEERING: doğrudan angularVelocity set et ---
        float minSteerSpeed = 0.05f;
        if (flatSpeed > minSteerSpeed && Mathf.Abs(steer) > 0.01f)
        {
            // hız oranı 0…1
            float speedRatio = 1-Mathf.Clamp01(flatSpeed/1.2f / (throttle >= 0f ? vehicleData.maxSpeed : vehicleData.maxReverseSpeed));

            // hedef açısal hız (y ekseninde), rad/s cinsinden
            float targetAngVel = steer                            // -1…1
                               * vehicleData.maxAngularVelocity  // örn. 2 rad/s
                               * speedRatio                     // hız arttıkça tam güç
                               * (throttle >= 0f ? 1f : -1.5f);   // geri sürüşte ters yön

            // mevcut açısal hızın sadece Y bileşenini değiştir
            Vector3 av = rb.angularVelocity;
            av.y = targetAngVel;
            rb.angularVelocity = av;
        }
        else
        {
            // dönerken tuş bırakıldıysa açısal hızı yavaşça sıfıra yaklaştırmak istersen:
            Vector3 av = rb.angularVelocity;
            av.y = Mathf.MoveTowards(av.y, 0f, rb.angularDrag * Time.fixedDeltaTime);
            rb.angularVelocity = av;
        }

        /*// --- STEERING: only if moving faster than a minimal speed ---
        float minSteerSpeed = 0.001f; // 0.5 units/sec’in altındayken dönme yok
        if (flatSpeed > minSteerSpeed)
        {
            // torque in N·m
            float speedRatio = Mathf.Clamp01(flatSpeed / vehicleData.maxSpeed);
            float torque = steer * vehicleData.steeringTorque * speedRatio;
            torque *= throttle >= 0 ? 1 : -1;

            // Açısal hızı clamp’le
            Vector3 av = rb.angularVelocity;
            av.y = Mathf.Clamp(av.y, -vehicleData.maxAngularVelocity, vehicleData.maxAngularVelocity);
            rb.angularVelocity = av;


            // Apply as continuous torque (respects mass/inertia)
            rb.AddTorque(transform.up * torque, ForceMode.Force);

            // Hıza bağlı turnRate (derece/saniye)
            *//*float speedRatio = Mathf.Clamp01(flatSpeed / vehicleData.maxSpeed);
            float turnRate = Mathf.Lerp(0,
                                          vehicleData.maxAngularVelocity,
                                          speedRatio);

            // Bu karede dönülecek açı
            float angle = steer * turnRate * Time.deltaTime;
            angle *= throttle >= 0 ? 1 : -1;

            // Rotate immediately
            Quaternion deltaRot = Quaternion.Euler(0f, angle, 0f);
            rb.MoveRotation(rb.rotation * deltaRot);*//*
        }*/

        // --- ACCELERATION / BRAKING via P-controller ---
        float forwardVel = transform.InverseTransformDirection(vel).z;
        float desiredSpeed = (throttle >= 0f
                              ? vehicleData.maxSpeed * throttle
                              : vehicleData.maxReverseSpeed * throttle);
        float speedError = desiredSpeed - forwardVel;
        float force = speedError * vehicleData.accelerationForce;

        rb.AddForce(transform.forward * force * Time.deltaTime,
                    ForceMode.Acceleration);

        //Debug.Log(rb.velocity.magnitude);

        // Fuel & condition consumption
        if (!Mathf.Approximately(throttle, 0f) && currentFuel > 0f && condition > 0f)
            ConsumeFuelAndCondition(Mathf.Abs(throttle));
    }


    private void ConsumeFuelAndCondition(float throttleAmount)
    {
        float fuelUse = vehicleData.fuelPerSecond * throttleAmount * Time.deltaTime;
        currentFuel = Mathf.Max(currentFuel - fuelUse, 0f);

        float condUse = vehicleData.conditionDecayRate * throttleAmount * Time.deltaTime;
        condition = Mathf.Max(condition - condUse, 0f);
    }

    // --- Original non-movement methods (unchanged) ---

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
    }
}
