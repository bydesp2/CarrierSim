using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public PlayerController playerController;
    public PhonePanelManager phonePanelManager; 
    public List<DeliveryTypeData> deliveryTypeDatas = new List<DeliveryTypeData>();
    public Text inGameOwnedAmountTx;
    public FoodSellerSc foodSellerSc;
    public ShoeStoreSc shoeStoreSc;
    public GameObject walletNotifElement;
    public Transform walletNotifParent;
    float currentOwnedMoney = 0;
    public float currentOhlBalance = 0;

    [Header("Vehicle Configs")]
    public Transform vehiclesOnSceneParent;
    public GameObject vehiclePrefab;


    public void Awake()
    {
        Instance = this;
        LoadSavedPlayerData();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LoadSavedPlayerData()
    {
        PlayerData playerData = SaveSystem.LoadPlayerData(); // SaveSystem içindeki yükleme fonksiyonu
        if(playerData != null )
        {
            playerController.SetPlayerData(playerData);

            currentOwnedMoney = playerData.ownedMoney;
            inGameOwnedAmountTx.text = GetUnitMoney(currentOwnedMoney) + "$";

            currentOhlBalance = playerData.ohlBalance;
            phonePanelManager.SetOhlBalance(currentOhlBalance);

            foreach(VehicleData data in playerData.vehiclesOnScene)
            {
                GameObject spawnedVehicle = Instantiate(vehiclePrefab, data.lastPosition, Quaternion.identity, vehiclesOnSceneParent);
                spawnedVehicle.GetComponent<VehicleController>().LoadVehicleFromSaveData(data);
            }
        }
    }

    void SavePlayerData()
    {
        PlayerData newPlayerData = playerController.GetPlayerData();
        newPlayerData.ohlBalance = currentOhlBalance;
        newPlayerData.ownedMoney = currentOwnedMoney;
        List<VehicleData> vehicleDataList = new List<VehicleData>();
        foreach(Transform vehicle in vehiclesOnSceneParent)
        {
            Debug.Log(vehicle.gameObject);
            Debug.Log(vehicle.GetComponent<VehicleController>());
            Debug.Log(newPlayerData);
            vehicleDataList.Add(vehicle.GetComponent<VehicleController>().GetVehicleSaveData());
        }
        newPlayerData.vehiclesOnScene = vehicleDataList;

        SaveSystem.SavePlayerData(newPlayerData);
    }

    public void IncreaseDecreaseOhlMoney(float amount)
    {
        currentOhlBalance += amount;
        phonePanelManager.SetOhlBalance(currentOhlBalance);
    }

    public void TransferOhlBalanceToPlayer()
    {
        EarnSpendMoney(currentOhlBalance);
        currentOhlBalance = 0;
        phonePanelManager.SetOhlBalance(currentOhlBalance);
    }

    public void EarnSpendMoney(float amount)
    {
        currentOwnedMoney += amount;
        GameObject newWalletNotifElement = Instantiate(walletNotifElement, walletNotifParent);
        newWalletNotifElement.transform.Find("Text").GetComponent<Text>().color = amount < 0 ? Color.red : Color.green;
        newWalletNotifElement.transform.Find("Text").GetComponent<Text>().text = amount < 0 ? "-" : "+";
        newWalletNotifElement.transform.Find("Text").GetComponent<Text>().text += GetUnitMoney(amount);
        inGameOwnedAmountTx.text = GetUnitMoney(currentOwnedMoney) + "$";
    }
     
    public bool TrySpendMoney(float amount)
    {
        if(currentOwnedMoney >= amount)
        {
            EarnSpendMoney(-amount);
            return true;
        }
        else
        {
            Debug.Log("No enough money for: " + amount + "$");
            return false;
        }
    }

    // Update is called once per frame
    void Update()
    {
       InputManager();
    }

    void InputManager()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            TaskManager.Instance.CreateJob();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TaskManager.Instance.PressedE();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            SavePlayerData();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            phonePanelManager.ShowHidePhone();
        }
    }
    public string GetUnitMoney(float amount)
    {
        // Geniþletilmiþ birim listesi (binlik sistemde)
        string[] suffixes = { "", "k", "m", "b", "t", "q", "qq", "s", "ss", "o", "n" };
        float value = amount;
        int index = 0;

        while (value >= 1000f && index < suffixes.Length - 1)
        {
            value /= 1000f;
            index++;
        }

        return value.ToString("F1") + suffixes[index];
    }
    
    // Old int version
    /*public string GetUnitMoney(int amount)
    {
        // Geniþletilmiþ birim listesi (binlik sistemde)
        string[] suffixes = { "", "k", "m", "b", "t", "q", "qq", "s", "ss", "o", "n" };
        double value = amount;
        int index = 0;

        while (value >= 1000 && index < suffixes.Length - 1)
        {
            value /= 1000.0;
            index++;
        }

        return value.ToString("F1") + suffixes[index];
    }*/
     
    public void EnterExitFoodSeller(bool enter)
    {
        foodSellerSc.OpenCloseFoodSellerPanel(enter);
    }
    public void EnterExitShoeStore(bool enter)
    {
        shoeStoreSc.OpenCloseShoeStorePanel(enter);
    }

    public void TryBuyFood(FoodTypeData food)
    {
        Debug.Log("Tried to buy " + food.foodName + " , for price " + food.price + "$.");
        if (TrySpendMoney(food.price))
        {
            playerController.ReduceHunger(food.hungerRecoverAmount);
            SavePlayerData();
            Debug.Log("Bought " + food.foodName + " for " + food.price + "$.");
        }
        else
        {
            Debug.Log("Not enough money for " + food.foodName);
        }
    }
    public void TryBuyShoe(ShoeData shoe)
    {
        Debug.Log("Tried to buy " + shoe.shoeName + " , for price " + shoe.price + "$.");
        if (TrySpendMoney(shoe.price))
        {
            playerController.SetCurrentShoe(shoe);
            playerController.ResetShoeDurability();
            SavePlayerData();
            Debug.Log("Bought " + shoe.shoeName + " for " + shoe.price + "$.");
        }
        else
        {
            Debug.Log("Not enough money for " + shoe.shoeName);
        }
    }

    public void TryRepairShoe()
    {
        Debug.Log("Tried to repair the current shoe for price " + GetShoeRepairCost() + "$.");
        if (TrySpendMoney(GetShoeRepairCost()))
        {
            playerController.ResetShoeDurability();
            SavePlayerData();
            Debug.Log("Not enough money to repair for price: " + GetShoeRepairCost() + "$.");
        }
        else
        {
            Debug.Log("Not enough money to repair for price: " + GetShoeRepairCost() + "$.");
        }
    }

    public float GetShoeRepairCost()
    {
        float cost = 0;
        ShoeData currentShoe = playerController.GetCurrentShoe();

        if (currentShoe != null)
        {
            cost = (currentShoe.price / 2) * (1 - playerController.currentShoeDurability);
        }

        return cost;
    }
}
