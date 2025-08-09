using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShoeStoreSc : MonoBehaviour
{
    public List<ShoeData> allShoeTypes;
    public GameObject shoeListElementPrefab;

    public GameObject shoeStorePanel;
    public Transform shoeListParent;

    private void Start()
    {
        InitShoeList();
    }

    void InitShoeList()
    {
        foreach (var shoe in allShoeTypes)
        {
            GameObject newElement = Instantiate(shoeListElementPrefab, shoeListParent);
            newElement.GetComponent<ShoeListElementSc>().SetListElement(shoe);
        }
    }

    public void OpenCloseShoeStorePanel(bool open)
    {
        shoeStorePanel.transform.Find("RepairButton").Find("CostTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(GameManager.Instance.GetShoeRepairCost()) + "$";
        shoeStorePanel.transform.Find("RepairButton").GetComponent<Button>().interactable = GameManager.Instance.playerController.GetCurrentShoe() != null;
        shoeStorePanel.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
