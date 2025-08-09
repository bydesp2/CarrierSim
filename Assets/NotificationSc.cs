using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotificationSc : MonoBehaviour
{
    private void Start()
    {
        GetComponent<CanvasGroup>().alpha = 0f;
        RectTransform textTransform = transform.Find("Text").GetComponent<RectTransform>();
        textTransform.DOLocalMove(Vector3.right*180, 0.5f).OnComplete(() => Destroy(gameObject, 1f));
        GetComponent<CanvasGroup>().DOFade(1,1);
    }
}
