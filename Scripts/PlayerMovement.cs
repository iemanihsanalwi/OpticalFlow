using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody rb;

    public float forwardForce = 2000f;

    void FixedUpdate()
    {
        // Add a forward force
        rb.AddForce(forwardForce * Time.deltaTime, 0, 0);
    }
}
