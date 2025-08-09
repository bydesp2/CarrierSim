using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

public class TakenIconSc : MonoBehaviour
{
    public DeliveryJob currentJob;
    public Transform timerImage;
    public Transform player;
    public float maxDistance = 50f;

    private float duration = 0;

    bool updateActive = true;

    private void FixedUpdate()
    {
        if (currentJob != null && updateActive)
        {
            float a = (currentJob.jobExpirationTime - Time.time) / duration;
            timerImage.localScale = Vector3.one * 3 * a;
            currentJob.takenJobsListObj.Find("TimerFill").GetComponent<Image>().fillAmount = a;
            currentJob.takenJobsListObj.Find("TimerTx").GetComponent<Text>().text = (duration * a).ToString("F1");

            currentJob.ohlTakenJobsListElement.Find("PlayerToDropoff").Find("DurTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(duration * a) + "s";


            if (Time.time >= currentJob.jobExpirationTime)
            {
                updateActive = false;
                TaskManager.Instance.JobTimeExpired(currentJob);
            }

            SetPosition();
        }
    }

    private void SetPosition()
    {
        // 1. Hedef ve player pozisyonlarýný al
        Vector3 targetPos = currentJob.dropoff.transform.position;
        Vector3 playerPos = player.position;

        // 2. Sadece XZ düzlemine indir ve clamp’le
        Vector3 dir = Vector3.ProjectOnPlane(targetPos - playerPos, Vector3.up);
        Vector3 offset = Vector3.ClampMagnitude(dir, maxDistance);
        Vector3 newPos = playerPos + offset;

        // 3. Y yüksekliðini koru
        newPos.y = transform.position.y;
        transform.position = newPos;
        currentJob.takenJobsListObj.Find("DistanceLeft").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(dir.magnitude) + "m";

        currentJob.ohlTakenJobsListElement.Find("PlayerToDropoff").Find("DistanceTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(dir.magnitude) + "m";
    }

    public void SetJob(DeliveryJob job)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        transform.Find("Icon").GetComponent<SpriteRenderer>().sprite = job.deliveryTypeData.artwork;
        duration = job.jobExpirationTime - Time.time;
        job.takenJobsListObj.Find("RewardTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(job.reward) + "$";
        currentJob = job;
    }

    private void OnDestroy()
    {
        if (currentJob != null)
        {
            if (currentJob.ohlTakenJobsListElement != null)
            {
                Destroy(currentJob.ohlTakenJobsListElement.gameObject);
            }
        }        
    }
}
