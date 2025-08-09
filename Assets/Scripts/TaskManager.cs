using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using System;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    public GameObject pickupJobPanel, dropoffJobPanel;
    public int maxTakenJobsLimit = 1;
    public int maxActiveJobsLimit = 1;

    [NonSerialized]public List<DeliveryJob> takenJobs = new List<DeliveryJob>();
    [NonSerialized]public List<DeliveryJob> activeJobs = new List<DeliveryJob>();

    private DeliveryJob currentPickupJobPanelJob;
    private DeliveryJob currentDropoffJobPanelJob;

    [Header("Job Dropoff Configs")]
    public GameObject takenTaskMapIconPrefab;
    public Transform takenTasksParent;

    [Header("Notification Configs")]
    public GameObject deliveredNotificationPrefab;
    public GameObject timeExpiredNotificationPrefab;
    public Transform notificationsParent;

    void Awake()
    {
        Instance = this;

        // Tüm DeliveryLocation bileşenlerini bir kez topla
        allLocations = new List<DeliveryLocation>(deliveryLocationsParent.GetComponentsInChildren<DeliveryLocation>());
    }


    public void TakeJob(DeliveryJob job)
    {
        Destroy(job.searchJobListElement.gameObject);
        job.pickup.ClearPickupJob();
        job.dropoff.AssignDropoffJob(job);

        activeJobs.Remove(job);
        takenJobs.Add(job);

        GameObject jobsListObj = Instantiate(takenJobsListObjPrefab, takenJobsListParent);
        job.takenJobsListObj = jobsListObj.transform;
        job.takenJobsListObj.Find("Artwork").GetComponent<Image>().sprite = job.deliveryTypeData.artwork;

        job.jobExpirationTime = Time.time + job.jobDuration;
        Vector3 spawnPoint = job.dropoff.transform.position;
        spawnPoint.y = 0;
        GameObject icon = Instantiate(takenTaskMapIconPrefab, spawnPoint, Quaternion.identity, takenTasksParent);
        job.dropoff.SetDropoffMinimapIcon(icon);
        GameManager.Instance.phonePanelManager.CreateOhlTakenJobsListElement(job);

        icon.GetComponent<TakenIconSc>().SetJob(job);
        job.takenMinimapIcon = icon;
        Debug.Log($"New dynamic job assigned: {job.pickup.locationName} -> {job.dropoff.locationName} | Reward: {job.reward}$");
    }

    public void TryDeliver(DeliveryLocation currentLocation)
    {
        for (int i = takenJobs.Count - 1; i >= 0; i--)
        {
            if (currentLocation == takenJobs[i].dropoff)
            {
                JobDelivered(takenJobs[i]);
            }
        }
    }

    public void JobDelivered(DeliveryJob job)
    {
        job.dropoff.ClearDropoffJob();
        takenJobs.Remove(job);

        bool timeBonusReward = job.jobExpirationTime - Time.time > job.jobDuration / 2;
        GameObject spawnedNotification = Instantiate(deliveredNotificationPrefab, job.takenJobsListObj.transform.position, Quaternion.identity, notificationsParent);
        spawnedNotification.transform.Find("Text").Find("Bonus").gameObject.SetActive(timeBonusReward);

        RemoveTakenJobsListElement(job.takenJobsListObj, true);

        HideDropoffJobPanel();

        float reward = timeBonusReward ? job.reward*2 : job.reward;

        //GameManager.Instance.EarnSpendMoney(reward);
        GameManager.Instance.IncreaseDecreaseOhlMoney(reward);

        if (timeBonusReward)
        {
            Debug.Log($"Delivered successfully 'WITH BONUS'! Earned {job.reward}$ X2= {2*job.reward}");
        }
        else
        {
            Debug.Log($"Delivered successfully 'WITHOUT BONUS'! Earned {job.reward}$");
        }
    }

    public void ShowPickupJobPanel(DeliveryLocation currentLocation)
    {
        currentPickupJobPanelJob = currentLocation.currentPickupJob;
        /*pickupJobPanel.transform.Find("LocationNameTx").GetComponent<Text>().text = currentLocation.locationName;
        pickupJobPanel.transform.Find("TakeJobBut").GetComponent<Button>().interactable = takenJobs.Count < maxTakenJobsLimit;
        pickupJobPanel.transform.Find("RewardTx").GetComponent<Text>().text = "Reward: " +  GameManager.Instance.GetUnitMoney(currentLocation.currentPickupJob.reward) + "$";
        pickupJobPanel.transform.Find("DurationTx").GetComponent<Text>().text = "Duration: " + currentLocation.currentPickupJob.jobDuration.ToString("F0") + " s.";
        pickupJobPanel.SetActive(true);*/
        GameManager.Instance.phonePanelManager.OpenPhonePickupPanel(currentLocation);
    }

    public void HidePickupJobPanel()
    {
        currentPickupJobPanelJob = null;
        GameManager.Instance.phonePanelManager.ShowHidePhone(false);
    }

    public void ShowDropoffJobPanel(DeliveryLocation currentLocation)
    {
        currentDropoffJobPanelJob = currentLocation.currentDropoffJob;
        /*dropoffJobPanel.transform.Find("LocationNameTx").GetComponent<Text>().text = currentLocation.locationName;

        dropoffJobPanel.transform.Find("RewardTx").GetComponent<Text>().text = "Reward: " + GameManager.Instance.GetUnitMoney(currentLocation.currentDropoffJob.reward) + "$";

        dropoffJobPanel.SetActive(true);

*/
        GameManager.Instance.phonePanelManager.OpenPhoneDropoffPanel(currentDropoffJobPanelJob);
    }

    public void HideDropoffJobPanel()
    {
        currentDropoffJobPanelJob = null;
        GameManager.Instance.phonePanelManager.ShowHidePhone(false);
        //dropoffJobPanel.SetActive(false);
    }

    public void PressedE()
    {
        if (currentPickupJobPanelJob != null)
        {
            PickupJobPanelTakeButton();
        }
        else if (currentDropoffJobPanelJob != null)
        {
            TryDeliver(currentDropoffJobPanelJob.dropoff);
        }
    }

    public void PickupJobPanelTakeButton()
    {
        Debug.Log("Pressed E, " + currentPickupJobPanelJob + " / " + takenJobs.Count);
        if (takenJobs.Count < maxTakenJobsLimit)
        {
            TakeJob(currentPickupJobPanelJob);
            HidePickupJobPanel();
        }
    }

    [Header("Job Create Configs")]
    public Transform deliveryLocationsParent;
    public GameObject activeTaskMapIconPrefab;
    public Transform activeTasksParent;

    public float baseRewardPerMeter = 0.1f;
    public Vector2 offerMinMax = Vector2.zero;
    public Vector2 jobDurMinMax = Vector2.zero;

    public GameObject takenJobsListObjPrefab;
    public Transform takenJobsListParent;

    // Önceden sadece editörde ayarlanan list yerine, dinamik ama sadece 1 kez alınacak liste:
    private List<DeliveryLocation> allLocations = new List<DeliveryLocation>();

    // New createJob for fixed double delivery bug
    public void CreateJob()
    {
        // 1. Genel kontrol: Yeterli lokasyon var mı ve aktif job limiti aşılmış mı
        if (allLocations.Count < 2 || activeJobs.Count >= maxActiveJobsLimit)
            return;

        // 2. Aktif işlerde kullanılan pickup lokasyonlarını topla
        HashSet<DeliveryLocation> usedPickups = new HashSet<DeliveryLocation>();
        foreach (var jb in activeJobs)
        {
            usedPickups.Add(jb.pickup);
        }

        // 3. Uygun pickup lokasyonlarını filtrele
        List<DeliveryLocation> availablePickups = new List<DeliveryLocation>();
        foreach (var loc in allLocations)
        {
            if (!usedPickups.Contains(loc))
                availablePickups.Add(loc);
        }

        if (availablePickups.Count == 0)
        {
            Debug.Log("No available pickup location for new job!");
            return;
        }

        // 4. Rastgele bir pickup seç
        DeliveryLocation pickup = availablePickups[UnityEngine.Random.Range(0, availablePickups.Count)];

        // 5. Aktif ve alınmış işlerde kullanılan dropoff lokasyonlarını topla
        HashSet<DeliveryLocation> usedDropoffs = new HashSet<DeliveryLocation>();
        foreach (var tjob in activeJobs)
        {
            usedDropoffs.Add(tjob.dropoff);
        }
        foreach (var tjob in takenJobs)
        {
            usedDropoffs.Add(tjob.dropoff);
        }

        // 6. Uygun dropoff lokasyonlarını filtrele
        List<DeliveryLocation> availableDropoffs = new List<DeliveryLocation>();
        foreach (var loc in allLocations)
        {
            if (loc != pickup && !usedDropoffs.Contains(loc))
                availableDropoffs.Add(loc);
        }

        if (availableDropoffs.Count == 0)
        {
            Debug.Log("No available dropoff location for new job!");
            return;
        }

        // 7. Rastgele bir dropoff seç
        DeliveryLocation dropoff = availableDropoffs[UnityEngine.Random.Range(0, availableDropoffs.Count)];

        // 8. Görev verilerini oluştur
        float dist = Vector3.Distance(pickup.transform.position, dropoff.transform.position);
        float reward = Mathf.Round(dist * baseRewardPerMeter);

        DeliveryJob job = new DeliveryJob
        {
            id = System.Guid.NewGuid().ToString(),
            pickup = pickup,
            dropoff = dropoff,
            distance = dist,
            reward = reward,
            offerExpirationTime = Time.time + UnityEngine.Random.Range(offerMinMax.x, offerMinMax.y),
            jobDuration = UnityEngine.Random.Range(jobDurMinMax.x, jobDurMinMax.y),
            deliveryTypeData = GameManager.Instance.deliveryTypeDatas[3] // Set type to "ENVELOPE" for TEST!
        };

        // 9. Aktif iş listesine ekle
        activeJobs.Add(job);
        pickup.AssignPickupJob(job);

        // 10. Minimap icon spawn et
        Vector3 spawnPoint = pickup.transform.position;
        spawnPoint.y = 0;
        GameObject icon = Instantiate(activeTaskMapIconPrefab, spawnPoint, Quaternion.identity, activeTasksParent);
        pickup.SetPickupMinimapIcon(icon);
        icon.GetComponent<ActiveIconSc>().SetJob(job);
        job.offerMinimapIcon = icon;

        GameManager.Instance.phonePanelManager.CreateOhlSearchJobListElement(job);

        Debug.Log("Job created! Active jobs count: " + activeJobs.Count);
    }


    // Old create job / With double delivery bug
    /*public void CreateJob()
    {
        if (allLocations.Count < 2 || activeJobs.Count >= maxActiveJobsLimit) return;

        // 1. Aktif işlerin pickup lokasyonlarını topla
        HashSet<DeliveryLocation> usedPickups = new HashSet<DeliveryLocation>();
        foreach (var jb in activeJobs)
        {
            usedPickups.Add(jb.pickup);
        }

        // 2. Kullanılabilir pickup lokasyonlarını filtrele
        List<DeliveryLocation> availablePickups = new List<DeliveryLocation>();
        foreach (var loc in allLocations)
        {
            if (!usedPickups.Contains(loc))
                availablePickups.Add(loc);
        }

        // 3. Eğer uygun pickup yoksa çık
        if (availablePickups.Count == 0)
        {
            Debug.Log("No available pickup location for new job!");
            return;
        }

        // 4. Uygun bir pickup seç ve farklı bir dropoff bul
        DeliveryLocation pickup = availablePickups[Random.Range(0, availablePickups.Count)];
        
        // Dropoff olarak kullanılabilecek uygun lokasyonları filtrele
        List<DeliveryLocation> availableDropoffs = new List<DeliveryLocation>();
        foreach (var loc in allLocations)
        {
            if (loc != pickup && loc.currentDropoffJob == null)
                availableDropoffs.Add(loc);
        }

        // Eğer uygun dropoff yoksa işlemi iptal et
        if (availableDropoffs.Count == 0)
        {
            Debug.Log("No available dropoff location for new job!");
            return;
        }

        // Uygun dropoff'lar içinden rastgele seç
        DeliveryLocation dropoff = availableDropoffs[Random.Range(0, availableDropoffs.Count)];

        *//*DeliveryLocation dropoff;
        do
        {
            dropoff = allLocations[Random.Range(0, allLocations.Count)];
        } while (dropoff == pickup);*//*

        float dist = Vector3.Distance(pickup.transform.position, dropoff.transform.position);
        float reward = Mathf.Round(dist * baseRewardPerMeter);

        DeliveryJob job = new DeliveryJob
        {
            id = System.Guid.NewGuid().ToString(),
            pickup = pickup,
            dropoff = dropoff,
            distance = dist,
            reward = reward,
            offerExpirationTime = Time.time + Random.Range(offerMinMax.x, offerMinMax.y),
            jobDuration = Random.Range(jobDurMinMax.x, jobDurMinMax.y),
            deliveryTypeData = GameManager.Instance.deliveryTypeDatas[3] // Set type to "ENVELOPE" for TEST!
        };

        activeJobs.Add(job);
        pickup.AssignPickupJob(job);

        Vector3 spawnPoint = pickup.transform.position;
        spawnPoint.y = 0;
        GameObject icon = Instantiate(activeTaskMapIconPrefab, spawnPoint, Quaternion.identity, activeTasksParent);
        pickup.SetPickupMinimapIcon(icon);
        icon.GetComponent<ActiveIconSc>().SetJob(job);
        job.offerMinimapIcon = icon;
        Debug.Log("Job created! Active jobs count: " + activeJobs.Count);
    }*/



    public void OfferTimeExpired(DeliveryJob job)
    {
        Destroy(job.searchJobListElement.gameObject);
        job.pickup.ClearPickupJob();
        if (currentPickupJobPanelJob == job)
        {
            HidePickupJobPanel();
        }
        activeJobs.Remove(job);
        Debug.Log("Job offer time expired! Active jobs count: " + activeJobs.Count);
    }

    public void OfferCanceled(DeliveryJob job)
    {
        Destroy(job.searchJobListElement.gameObject);
        job.pickup.ClearPickupJob();
        if (currentPickupJobPanelJob == job)
        {
            HidePickupJobPanel();
        }
        activeJobs.Remove(job);
        Debug.Log("Job offer time expired! Active jobs count: " + activeJobs.Count);
    }


    public void JobTimeExpired(DeliveryJob job)
    {
        job.dropoff.ClearDropoffJob();
        if (currentDropoffJobPanelJob == job)
        {
            HideDropoffJobPanel();
        }
        takenJobs.Remove(job);
        Instantiate(timeExpiredNotificationPrefab, job.takenJobsListObj.transform.position, Quaternion.identity, notificationsParent);
        RemoveTakenJobsListElement(job.takenJobsListObj, false);
        Debug.Log("Taken job time expired! Taken jobs count: " + takenJobs.Count);
    }

    public void RemoveTakenJobsListElement(Transform element, bool success)
    {
        element.Find("PanelBg").GetComponent<Image>().color = success ? Color.green : Color.red;

        element.GetComponent<CanvasGroup>().DOFade(0, 1.5f).OnComplete(() =>
        {
            Destroy(element.gameObject);
        });
    }
}
