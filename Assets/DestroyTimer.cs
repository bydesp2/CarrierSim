using UnityEngine;

public class DestroyTimer : MonoBehaviour
{
    public float duration = 2f;

    float timer = 0;

    // Update is called once per frame
    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if(timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
