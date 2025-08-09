using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodSellerSc : MonoBehaviour
{
    public List<FoodTypeData> allFoodTypes;
    public GameObject foodListElementPrefab;

    public GameObject foodSellerPanel;
    public Transform foodListParent;

    private void Start()
    {
        InitFoodList();
    }

    void InitFoodList()
    {
        foreach (var food in allFoodTypes)
        {
            GameObject newElement = Instantiate(foodListElementPrefab, foodListParent);
            newElement.GetComponent<FoodListElementSc>().SetListElement(food);
        }
    }

    public void OpenCloseFoodSellerPanel(bool open)
    {
        foodSellerPanel.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
