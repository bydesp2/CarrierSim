using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewShoeType", menuName = "Shoe/Shoe Type Data")]
public class ShoeData : ScriptableObject
{
    public string itemId;
    public string shoeName;

    public Sprite artwork;

    public float price;
    public float stamina;

    public float speedBoost = 1.1f;
    public float energyEffect = 0.9f;
}
