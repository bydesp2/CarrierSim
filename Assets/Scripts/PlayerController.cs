using DG.Tweening;
using System;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public bool moveInputEnabled = false;
    public float moveSpeed = 5f;
    public float defMoveSpeed = 5f;
    public float boostedMoveSpeedMultiplier = 2f;
    public Transform orientation;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool runBoosted = false;

    [NonSerialized]public bool isCarrying = false;

    [Header("Energy Configs")]
    public Slider energySlider;
    public float defEnergyUsePerS = 0.15f;
    public float defEnergyRegenPerS = 0.1f;
    private float currentEnergyUsePerS = 0.15f;
    private float currentEnergyRegenPerS = 0.1f;
    private float currentEnergy = 1f;

    [Header("Hunger Configs")]
    public Slider hungerSlider;
    public Image hungerCanvas;
    public Color hungerWarnColor;
    public float defHungerUsePerS = 0.01f;
    public float walkHungerUseMultiplier = 1.5f;
    public float runHungerUseMultiplier = 2f;
    public float hungerWalkEffectPoint = 0.5f;
    public float hungerWalkEffectMultiplier = 0.5f;
    private float currentHungerUsePerS = 0.01f;
    private float currentHunger = 1f;

    [Header("Shoe Configs")]
    public Slider shoeSlider;
    public float walkShoeUsePerS = 0.003f;
    public float runShoeUsePerS = 0.005f;
    private ShoeData currentShoe = null;
    [NonSerialized] public float currentShoeDurability = 1f;


    [Header("Player Stats")]
    public float playerOutfitGrade = 0f;


    [Header("Vehicle Configs")]
    public VehicleController currentVehicleController;

    private bool isWalking = false;
    public void SetPlayerData(PlayerData playerData)
    {
        currentHunger = playerData.hunger;
        currentEnergy = playerData.energy;

        // Init shoe
        if(playerData.equippedShoes != null)
        {
            string shoeItemId = playerData.equippedShoes.itemId;
            foreach (ShoeData shoe in GameManager.Instance.shoeStoreSc.allShoeTypes)
            {
                if (shoe.itemId == shoeItemId)
                {
                    SetCurrentShoe(shoe);
                    Debug.Log(shoe.shoeName + " is imported as current shoe.");
                    break;
                }
            }
            currentShoeDurability = playerData.equippedShoes.durability;
        }
    }

    public PlayerData GetPlayerData()
    {
        ItemData shoeData = null;
        // Get shoe
        if (currentShoe != null)
        {
            shoeData = new ItemData()
            {
                itemId = currentShoe.itemId,
                durability = currentShoeDurability
            };
        }

        PlayerData newPlayerData = new PlayerData()
        {
            hunger = currentHunger,
            energy = currentEnergy,
            equippedShoes = shoeData
        };

        return newPlayerData;
    }


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        moveSpeed = defMoveSpeed;
        currentHungerUsePerS  = defHungerUsePerS;
        currentEnergyUsePerS = defEnergyUsePerS;
        currentEnergyRegenPerS = defEnergyRegenPerS;
    }

    void Update()
    {
        if (moveInputEnabled && currentVehicleController == null)
        {
            MovementInput();
        }
        else if(currentVehicleController != null)
        {
            transform.position = currentVehicleController.playerPlace.position;
            transform.localRotation = currentVehicleController.playerPlace.rotation;
            isWalking = false;
        }
    }

    void MovementInput()
    {
        SetBoostedMoveSpeed();

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Ham input yönü (kamera baðýmsýz)
        Vector3 input = new Vector3(h, 0, v).normalized;
        // input'u orientation'ýn Y açýsýyla döndür
        Quaternion rotation = Quaternion.Euler(0f, orientation.eulerAngles.y, 0f);
        moveDirection = rotation * input;

        moveSpeed = defMoveSpeed
                    * (runBoosted ? boostedMoveSpeedMultiplier : 1)
                    * (hungerWalkEffectPoint > currentHunger ? hungerWalkEffectMultiplier : 1)
                    * (currentShoe != null ? currentShoe.speedBoost : 1); // Effected by hunger
        rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.deltaTime);
        isWalking = input.magnitude > 0;
    }



    void FixedUpdate()
    {
        EnergyManager();
        HungerManager();
        if (currentShoe != null)
        {
            ShoeDurabilityManager();  
        }
/*
        if (moveInputEnabled)
        {
            if (currentVehicleController != null)
            {
                currentVehicleController.UpdateRemoteControl(moveDirection * Time.fixedDeltaTime);
                transform.position = currentVehicleController.playerPlace.position;
                transform.localRotation = currentVehicleController.playerPlace.rotation;
            }
            else
            {
                moveSpeed = defMoveSpeed
                            * (runBoosted ? boostedMoveSpeedMultiplier : 1)
                            * (hungerWalkEffectPoint > currentHunger ? hungerWalkEffectMultiplier : 1)
                            * (currentShoe != null ? currentShoe.speedBoost : 1); // Effected by hunger
                rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
            }
        }*/
    }

    void EnergyManager()
    {
        // Reduce energy
        if(!runBoosted && currentEnergy < 1f)
        {
            //Debug.Log("reduce amount per sec: " + (currentEnergyRegenPerS * Mathf.Clamp(currentHunger, 0.2f, 1)));
            currentEnergy += currentEnergyRegenPerS * Time.fixedDeltaTime * Mathf.Clamp(currentHunger, 0.2f, 1) / (2 - currentShoeDurability); // Effected by hunger
            if(currentEnergy > 1)
            {
                currentEnergy = 1;
            }
        }
        // Use energy
        else if (runBoosted)
        {
            currentEnergy -= currentEnergyUsePerS * Time.fixedDeltaTime * (currentShoe != null ? currentShoe.energyEffect : 1) * (2-currentShoeDurability);
            if(currentEnergy <= 0)
            {
                runBoosted = false;
            }
        }
        energySlider.value = currentEnergy;
    }

    bool hungerWarning = false;
    private Tween hungerCanvasTween;
    void HungerManager()
    {
        float usage = currentHungerUsePerS;
        if(runBoosted)
        {
            usage *= runHungerUseMultiplier;
        }
        else if (isWalking)
        {
            usage *= walkHungerUseMultiplier;
        }

        currentHunger = Mathf.Clamp01(currentHunger - (usage * Time.fixedDeltaTime));
        hungerSlider.value = currentHunger;

        if((!hungerWarning && hungerWalkEffectPoint > currentHunger) || (hungerWarning && hungerWalkEffectPoint <= currentHunger))
        {
            SetHungerWarningState();
        }
    }

    void SetHungerWarningState()
    {
        hungerWarning = !hungerWarning;
        // Mevcut tween varsa kill et
        hungerCanvasTween?.Kill();

        if (hungerWarning)
        {
            // Tween'i baþlat
            hungerCanvasTween = hungerCanvas.DOColor(hungerWarnColor, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            hungerCanvas.color = Color.white;
        }
    }

    void ShoeDurabilityManager()
    {
        float usage = 0;
        if (runBoosted)
        {
            usage = runShoeUsePerS * Mathf.Clamp01(1 - currentShoe.stamina);
        }
        else if (isWalking)
        {
            usage = walkShoeUsePerS * Mathf.Clamp01(1 - currentShoe.stamina);
        }
        currentShoeDurability = Mathf.Clamp01(currentShoeDurability - (usage * Time.fixedDeltaTime));
        //Debug.Log("usage: " + usage + " | currentDur: " + currentShoeDurability);
        shoeSlider.value = currentShoeDurability;
    }

    public void ReduceHunger(float amount)
    {
        currentHunger = Mathf.Clamp01(currentHunger+amount);        
    }

    public void SetCurrentShoe(ShoeData shoe)
    {
        currentShoe = shoe;
        shoeSlider.transform.parent.gameObject.SetActive(currentShoe != null);
    }

    public ShoeData GetCurrentShoe()
    {
        return currentShoe;
    }

    public void ResetShoeDurability()
    {
        currentShoeDurability = 1;
    }

    public void GetInOutVehicle(VehicleController? setController = null)
    {
        moveInputEnabled = false;
        rb.velocity = Vector3.zero;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        // Set cuurent vehicle
        if (setController != null)
        {
            currentVehicleController = setController;
            currentVehicleController.PlayerGetInOut(true);
            runBoosted = false;

            rb.isKinematic = true;
            //rb.detectCollisions = false;
            capsule.enabled = false;

            //transform.SetParent(currentVehicleController.playerPlace);
            transform.position = currentVehicleController.playerPlace.position;
            transform.localRotation = currentVehicleController.playerPlace.rotation;

/*
            GetComponent<CapsuleCollider>().enabled = false;
            transform.parent = currentVehicleController.playerPlace;
            transform.localPosition = Vector3.zero;*/
        }
        // Set currentVehicle empty
        else
        {
            // Araçtan in
            //transform.SetParent(null);
            transform.position = currentVehicleController.transform.position + Vector3.left * 3 + Vector3.up * 0.5f;

            rb.isKinematic = false;
            rb.detectCollisions = true;
            capsule.enabled = true;

            currentVehicleController.PlayerGetInOut(false);
            currentVehicleController = null;
        }

        moveInputEnabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PickupZone"))
        {
            var location = other.transform.parent.GetComponent<DeliveryLocation>();
            if (location != null)
            {
                Debug.Log("Pickup at: " + location.locationName);
                EnterPickup(location);
            }
        }
        else if (other.CompareTag("DropoffZone"))
        {
            var location = other.transform.parent.GetComponent<DeliveryLocation>();
            if (location != null)
            {
                Debug.Log("Delivered to: " + location.locationName);
                EnterDropoff(location);
            }
        }
        else if (other.CompareTag("FoodSeller"))
        {
            GameManager.Instance.EnterExitFoodSeller(true);
        }
        else if (other.CompareTag("ShoeStore"))
        {
            GameManager.Instance.EnterExitShoeStore(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PickupZone"))
        {
            var location = other.transform.parent.GetComponent<DeliveryLocation>();
            if (location != null)
            {
                Debug.Log("Pickup at: " + location.locationName);
                ExitPickup(location);
            }
        }
        else if (other.CompareTag("DropoffZone"))
        {
            var location = other.transform.parent.GetComponent<DeliveryLocation>();
            if (location != null)
            {
                Debug.Log("Delivered to: " + location.locationName);
                ExitDropoff(location);
            }
        }
        else if (other.CompareTag("FoodSeller"))
        {
            GameManager.Instance.EnterExitFoodSeller(false);
        }
        else if (other.CompareTag("ShoeStore"))
        {
            GameManager.Instance.EnterExitShoeStore(false);
        }
    }

    public void SetBoostedMoveSpeed()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && currentEnergy > 0 && currentVehicleController == null)
        {
            runBoosted = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            runBoosted = false;
        }
    }

    void EnterPickup(DeliveryLocation location)
    {
        TaskManager.Instance.ShowPickupJobPanel(location);
    }

    void ExitPickup(DeliveryLocation location)
    {
        TaskManager.Instance.HidePickupJobPanel();
    }


    void EnterDropoff(DeliveryLocation location)
    {
        TaskManager.Instance.ShowDropoffJobPanel(location);
    }

    void ExitDropoff(DeliveryLocation location)
    {
        TaskManager.Instance.HideDropoffJobPanel();
    }
} 