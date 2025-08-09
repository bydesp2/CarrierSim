using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconMinimapFallower : MonoBehaviour
{
    Transform player;
    public float maxDistance = 10.57f;
    private void Start()
    {
        player = GameManager.Instance.playerController.transform;
        transform.Find("Icon").rotation = Quaternion.Euler(Vector3.right*90);
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        SetPosition();
    }

    private void SetPosition()
    {
        // 1. Hedef ve player pozisyonlarýný al
        Vector3 targetPos = transform.parent.position;
        Vector3 playerPos = player.position;

        // 2. Sadece XZ düzlemine indir ve clamp’le
        Vector3 dir = Vector3.ProjectOnPlane(targetPos - playerPos, Vector3.up);
        Vector3 offset = Vector3.ClampMagnitude(dir, maxDistance);
        Vector3 newPos = playerPos + offset;

        // 3. Y yüksekliðini koru
        newPos.y = transform.position.y;
        transform.position = newPos;
    }
}
