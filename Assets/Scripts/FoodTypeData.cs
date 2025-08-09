using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodType", menuName = "Food/Food Type Data")]
public class FoodTypeData : ScriptableObject
{
    public string foodName;
    public float price = 10f;
    public float hungerRecoverAmount = 0.5f;

    [Header("Visuals")]
    public Sprite artwork;

    [Header("Optional Info")]
    [TextArea] public string description;
}
