using UnityEngine;

public class Movement : MonoBehaviour
{
    public float normalSpeed;
    public float shiftSpeed;

    void Update()
    { 
        bool shift = Input.GetKey(KeyCode.LeftShift);

        float speed = shift ? shiftSpeed : normalSpeed;

        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector3 movement = new Vector3(input.x, 0, input.y) * speed * Time.deltaTime;

        transform.Translate(movement);
    }
}
