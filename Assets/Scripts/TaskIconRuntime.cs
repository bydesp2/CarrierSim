using UnityEngine;

public class TaskIconRuntime : MonoBehaviour
{
    private DeliveryJob jobData;

    public void Initialize(DeliveryJob job)
    {
        jobData = job;
    }

    void OnMouseDown()
    {
        TaskManager.Instance.TakeJob(jobData);
        Destroy(gameObject);
    }
}