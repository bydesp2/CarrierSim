using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class FoodListElementSc : MonoBehaviour
{
    public FoodTypeData foodType;

    public void SetListElement(FoodTypeData foodType)
    {
        this.foodType = foodType;
        
        transform.Find("FoodName").GetComponent<Text>().text = foodType.foodName;
        transform.Find("Icon").GetComponent<Image>().sprite = foodType.artwork;
        transform.Find("PriceBg").Find("PriceTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(foodType.price) + "$";
    }

    public void BuyFood()
    {
        GameManager.Instance.TryBuyFood(foodType);
    }
}
