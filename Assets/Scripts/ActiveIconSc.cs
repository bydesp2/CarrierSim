using UnityEngine;

public class ActiveIconSc : MonoBehaviour
{
    public DeliveryJob currentJob;
    public Transform timerImage;
    public Transform player;
    public float maxDistance = 50f;

    private float duration = 0;
    private void FixedUpdate()
    {
        if (currentJob != null)
        {
            float a = (currentJob.offerExpirationTime - Time.time) / duration;
            timerImage.localScale = Vector3.one * 3 * a;

            if (Time.time >= currentJob.offerExpirationTime)
            {
                TaskManager.Instance.OfferTimeExpired(currentJob);
            }
            SetPosition();
        }
    }
    private void SetPosition()
    {
        // 1. Hedef ve player pozisyonlarýný al
        Vector3 targetPos = currentJob.pickup.transform.position;
        Vector3 playerPos = player.position;

        // 2. Sadece XZ düzlemine indir ve clamp’le
        Vector3 dir = Vector3.ProjectOnPlane(targetPos - playerPos, Vector3.up);
        Vector3 offset = Vector3.ClampMagnitude(dir, maxDistance);
        Vector3 newPos = playerPos + offset;

        // 3. Y yüksekliðini koru
        newPos.y = transform.position.y;
        transform.position = newPos;
    }



    public void SetJob(DeliveryJob job)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        duration = job.offerExpirationTime - Time.time;
        currentJob = job;
    }
}
