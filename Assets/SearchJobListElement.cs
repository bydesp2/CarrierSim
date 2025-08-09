using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SearchJobListElement : MonoBehaviour
{
    DeliveryJob currentJob;
    Transform player;

    private float duration = 0;

    private void FixedUpdate()
    {
        if (currentJob != null)
        {
            transform.Find("PlayerToPickup").Find("DistanceTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney((Vector3.Distance(player.position, currentJob.pickup.transform.position))) + "m";


            float a = (currentJob.offerExpirationTime - Time.time) / duration;
            transform.Find("PlayerToPickup").Find("DurTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(currentJob.offerExpirationTime - Time.time) + "s";
        }
    }
    public void SetJob(DeliveryJob job)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        transform.Find("JobIcon").GetComponent<Image>().sprite = job.deliveryTypeData.artwork;
        transform.Find("JobDistance").Find("DistanceTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(job.distance) + "m";
        transform.Find("RewardTx").GetComponent<Text>().text = "Reward: " + GameManager.Instance.GetUnitMoney(job.reward) + "$";

        duration = job.offerExpirationTime - Time.time;
        currentJob = job;
    }

    public void CancelJob()
    {
        TaskManager.Instance.OfferCanceled(currentJob);
    }
}
