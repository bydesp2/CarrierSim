using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            TaskManager.Instance.CreateJob();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TaskManager.Instance.PressedE();
        }
    }
}
