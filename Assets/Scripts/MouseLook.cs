using Cinemachine;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public Transform playerBody;  // Y ekseninde döner
    public Transform head;        // X ekseninde döner

    public float mouseSensitivity = 100f;
    public bool invertY = false;

    private float xRotation = 0f;

    public CinemachineVirtualCamera fpsCam;
    public CinemachineVirtualCamera tpsCam;

    private bool isFPS = false;

    void Start()
    {
        ActivateFPS(false);
    }

    void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= invertY ? -mouseY : mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            head.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            playerBody.Rotate(Vector3.up * mouseX);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            isFPS = !isFPS;
            ActivateFPS(isFPS);
        }
    }

    void ActivateFPS(bool activate)
    {
        fpsCam.Priority = activate ? 10 : 0;
        tpsCam.Priority = activate ? 0 : 10;
    }
}