using UnityEngine;

public class DeliveryJob
{
    public string id;
    public DeliveryLocation pickup;
    public DeliveryLocation dropoff;
    public float reward;
    public float distance;
    public float offerExpirationTime;
    public float jobExpirationTime;
    public float jobDuration;
    public GameObject offerMinimapIcon, takenMinimapIcon;
    public DeliveryTypeData deliveryTypeData;
    public SearchJobListElement searchJobListElement;
    public Transform ohlTakenJobsListElement;
    public Transform takenJobsListObj;
}