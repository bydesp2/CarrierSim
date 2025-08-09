using UnityEngine;

public class DeliveryLocation : MonoBehaviour
{
    public string locationName;

    public bool pickupActive = false;
    public bool dropoffActive = false;

    public DeliveryJob currentPickupJob = null;
    public DeliveryJob currentDropoffJob = null;

    public GameObject pickupMinimapIcon, dropoffMinimapIcon;

    public void AssignPickupJob(DeliveryJob deliveryJob)
    {
        currentPickupJob = deliveryJob;
        SetActivePickup(true);
    }

    public void AssignDropoffJob(DeliveryJob deliveryJob)
    {
        currentDropoffJob = deliveryJob;
        SetActiveDropoff(true);
    }

    public void ClearPickupJob()
    {
        currentPickupJob = null;
        SetActivePickup(false);
        Destroy(pickupMinimapIcon);
    }

    public void ClearDropoffJob()
    {
        currentDropoffJob = null;
        SetActiveDropoff(false);
        Destroy(dropoffMinimapIcon);
    }
    public void JobDropedoff()
    {
        currentDropoffJob = null;
        SetActiveDropoff(false);
    }

    public void SetActivePickup(bool active)
    {
        pickupActive = active;
        transform.Find("PickupZone").gameObject.SetActive(active);
    }

    public void SetActiveDropoff(bool active)
    {
        dropoffActive = active;
        transform.Find("DropoffZone").gameObject.SetActive(active);
    }

    public void SetPickupMinimapIcon(GameObject icon)
    {
        pickupMinimapIcon = icon;
    }
    public void SetDropoffMinimapIcon(GameObject icon)
    {
        dropoffMinimapIcon = icon;
    }
}