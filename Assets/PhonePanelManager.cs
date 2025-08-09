using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Unity.Jobs;

public class PhonePanelManager : MonoBehaviour
{
    public Transform panelsParent;
    private Dictionary<Transform, Tween> activeFadeTweens = new Dictionary<Transform, Tween>();
    private RectTransform phoneUIRect;
    private Tween moveTween;

    private DeliveryJob currentPickupJobPanelJob;
    private DeliveryJob currentDropoffJobPanelJob;

    bool phoneOnScreen = false;
    float ohlBalance = 0;

    private void Awake()
    {
        phoneUIRect = transform.GetChild(0).GetComponent<RectTransform>(); // PhoneUIObject
    }
    public void ShowHidePhone()
    {
        ShowHidePhone(!phoneOnScreen);
    }
    public void ShowHidePhone(bool show)
    {
        phoneOnScreen = show;
        DoTweenFadeOutAllPanels();
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;


        if (moveTween != null && moveTween.IsActive()) moveTween.Kill();

        float targetX = show ? -200 : 200;

        moveTween = phoneUIRect.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.OutQuad);
    }

    void OpenPhoneWithPanel(string panelName)
    {
        ShowHidePhone(true);
        switch (panelName)
        {
            case "PickupJobPanel":
                DoTweenFadeInPanel(panelsParent.Find("PickupJobPanel"));
                break;
            case "DropoffJobPanel":
                DoTweenFadeInPanel(panelsParent.Find("DropoffJobPanel"));
                break;
        }
    }

    public void AppButtonClicked(string appKey)
    {
        DoTweenFadeOutAllPanels();
        switch (appKey)
        {
            case "OHL":
                DoTweenFadeInPanel(panelsParent.Find("OHLPanel"));
                OhlPanelButtonPressed("profile");
                break;
        }
    }

    public void OpenPhonePickupPanel(DeliveryLocation currentLocation)
    {
        SetPickupJobPanelUI(currentLocation);
        OpenPhoneWithPanel("PickupJobPanel");
    }
    public void OpenPhoneDropoffPanel(DeliveryJob job)
    {
        SetDropoffJobPanelUI(job);
        OpenPhoneWithPanel("DropoffJobPanel");
    }

    void DoTweenFadeInPanel(Transform panel)
    {
        if (activeFadeTweens.ContainsKey(panel))
        {
            activeFadeTweens[panel].Kill();
            panel.gameObject.SetActive(false);
            activeFadeTweens.Remove(panel);
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = panel.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        panel.gameObject.SetActive(true);

        Tween tween = cg.DOFade(1f, 0.4f).SetEase(Ease.OutQuad);
        activeFadeTweens[panel] = tween;
    }

    void DoTweenFadeOutPanel(Transform panel)
    {
        if (activeFadeTweens.ContainsKey(panel))
        {
            activeFadeTweens[panel].Kill();
            activeFadeTweens.Remove(panel);
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = panel.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 1f;

        Tween tween = cg.DOFade(0f, 0.25f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            panel.gameObject.SetActive(false);
        });

        activeFadeTweens[panel] = tween;
    }

    void DoTweenFadeOutAllPanels()
    {
        foreach (Transform panel in panelsParent)
        {
            if (panel.gameObject.activeSelf)
                DoTweenFadeOutPanel(panel);
        }
    }

    public void SetPickupJobPanelUI(DeliveryLocation currentLocation)
    {
        GameObject pickupJobPanel = panelsParent.Find("PickupJobPanel").gameObject;

        currentPickupJobPanelJob = currentLocation.currentPickupJob;
        pickupJobPanel.transform.Find("Pickup").Find("PickupName").GetComponent<Text>().text = currentLocation.locationName;
        pickupJobPanel.transform.Find("Dropoff").Find("DropoffName").GetComponent<Text>().text = currentPickupJobPanelJob.dropoff.locationName;
        pickupJobPanel.transform.Find("TakeJobBut").GetComponent<Button>().interactable = TaskManager.Instance.takenJobs.Count < TaskManager.Instance.maxTakenJobsLimit;
        pickupJobPanel.transform.Find("Duration").Find("DurationAmount").GetComponent<Text>().text = currentLocation.currentPickupJob.jobDuration.ToString("F0") + " s.";
        pickupJobPanel.transform.Find("Reward").Find("RewardAmount").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(currentLocation.currentPickupJob.reward) + "$";
    }
    public void SetDropoffJobPanelUI(DeliveryJob job)
    {
        GameObject dropoffJobPanel = panelsParent.Find("DropoffJobPanel").gameObject;

        currentDropoffJobPanelJob = job;
        dropoffJobPanel.transform.Find("Pickup").Find("PickupName").GetComponent<Text>().text = currentDropoffJobPanelJob.pickup.locationName;
        dropoffJobPanel.transform.Find("Dropoff").Find("DropoffName").GetComponent<Text>().text = currentDropoffJobPanelJob.dropoff.locationName;
        dropoffJobPanel.transform.Find("Reward").Find("RewardAmount").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(currentDropoffJobPanelJob.reward) + "$";
    }

    [Header("OHL Panel Configs")]
    public Text ohlPanelTitleText;
    public Text ohlBalanceText;
    public Transform ohlPagesParent, ohlSearchJobListParent, ohlTakenJobsListParent;
    public GameObject ohlSearchJobListElementPrefab, ohlTakenJobsListElementPrefab;

    public void OhlPanelButtonPressed(string buttonKey)
    {
        switch (buttonKey)
        {
            case "searchJob":
                ohlPanelTitleText.text = "SEARCH JOB";
                break;

            case "takenJobs":
                ohlPanelTitleText.text = "TAKEN JOBS";
                break;

            case "profile":
                ohlPanelTitleText.text = "PROFILE";
                break;
        }
        OhlPageSwitchTo(buttonKey);
    }

    public void OhlPageSwitchTo(string pageKey)
    {
        // Set off all pages
        foreach(Transform page in ohlPagesParent)
        {
            page.gameObject.SetActive(false);
        }

        // Set on the target page
        switch (pageKey)
        {
            case "searchJob":
                ohlPagesParent.Find("SearchJobPage").gameObject.SetActive(true);
                break;

            case "takenJobs":
                ohlPagesParent.Find("TakenJobsPage").gameObject.SetActive(true);
                break;

            case "profile":
                ohlPagesParent.Find("ProfilePage").gameObject.SetActive(true);
                break;
        }
    }

    public void CreateOhlSearchJobListElement(DeliveryJob job)
    {
        GameObject createdElement = Instantiate(ohlSearchJobListElementPrefab, ohlSearchJobListParent);
        job.searchJobListElement = createdElement.GetComponent<SearchJobListElement>();
        createdElement.GetComponent<SearchJobListElement>().SetJob(job);
    }
    public void CreateOhlTakenJobsListElement(DeliveryJob job)
    {
        GameObject createdElement = Instantiate(ohlTakenJobsListElementPrefab, ohlTakenJobsListParent);
        job.ohlTakenJobsListElement = createdElement.transform;

        createdElement.transform.Find("JobIcon").GetComponent<Image>().sprite = job.deliveryTypeData.artwork;
        createdElement.transform.Find("PlayerToDropoff").Find("DistanceTx").GetComponent<Text>().text = GameManager.Instance.GetUnitMoney(job.distance) + "m";
        createdElement.transform.Find("RewardTx").GetComponent<Text>().text = "Reward: " + GameManager.Instance.GetUnitMoney(job.reward) + "$";
    }

    public void SetOhlBalance(float amount)
    {
        ohlBalance = amount;
        ohlBalanceText.text = GameManager.Instance.GetUnitMoney(ohlBalance) + "$";
    }
}
