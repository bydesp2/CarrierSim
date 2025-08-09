using UnityEngine;

[CreateAssetMenu(fileName = "NewDeliveryType", menuName = "Delivery/Delivery Type Data")]
public class DeliveryTypeData : ScriptableObject
{
    public DeliveryType deliveryType;

    [Header("Visuals")]
    public Sprite artwork;

    [Header("Optional Info")]
    [TextArea] public string description;
}
public enum DeliveryType
{
    Envelope,   // Zarf
    Box,        // Kutu
    BoxMini,
    Crate,      // Sandýk
    Fragile,    // Kýrýlabilir
    Oversized   // Hacimli / Büyük Boy
}
