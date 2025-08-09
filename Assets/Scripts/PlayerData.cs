using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public float ownedMoney;
    public float ohlBalance;

    public float hunger;
    public float energy;

    public ItemData equippedShoes;
    public List<ItemData> inventoryItems;
    public List<VehicleData> vehiclesOnScene;
}
