using UnityEngine;

public class Camera : MonoBehaviour
{
    public float sensitivity;

    float pitch;
    float yaw;

    void Update()
    {
        yaw += Input.GetAxis("Mouse X");
        pitch -= Input.GetAxis("Mouse Y");

        transform.eulerAngles = new Vector3(pitch, yaw, 0) * sensitivity;
    }
}
