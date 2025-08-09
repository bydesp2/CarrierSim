using UnityEngine;
using UnityEngine.UI;

public class ShoeListElementSc : MonoBehaviour
{
    public ShoeData shoeData;


    public void SetListElement(ShoeData shoeType)
    {
        shoeData = shoeType;

        transform.Find("ShoeName").GetComponent<Text>().text = shoeData.shoeName;
        transform.Find("Icon").GetComponent<Image>().sprite = shoeData.artwork;
        transform.Find("PriceBg").Find("PriceTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(shoeData.price) + "$";
    }

    public void BuyShoe()
    {
        GameManager.Instance.TryBuyShoe(shoeData);
    }
}
