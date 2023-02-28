using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float moveSpeed = 100;
    public float rotationSpeed = 30;

    private Vector3 offset;
    private Transform followTarget;
    private bool isFollow = false;
    private float angleX = 0;
    private float angleY = 0;

    public void SetTarget(Transform target)
    {
        this.followTarget = target;
        if (this.followTarget == null)
        {
            this.isFollow = false;
        }
        else
        {
            this.isFollow = true;
            this.offset = this.transform.position - target.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            this.angleX += Input.GetAxis("Mouse Y") * this.rotationSpeed * Time.deltaTime;
            this.angleX = Mathf.Clamp(this.angleX, -45, 45);
            this.angleY += Input.GetAxis("Mouse X") * this.rotationSpeed * Time.deltaTime;
            this.transform.rotation = Quaternion.Euler(new Vector3(this.angleX, this.angleY, 0));
        }

        if (isFollow)
        {
            this.transform.position = this.followTarget.position + offset;
        }
        else
        {
            Vector3 translation = this.transform.right * Input.GetAxis("Horizontal") + this.transform.forward * Input.GetAxis("Vertical");
            if (Input.GetKey(KeyCode.Q))
            {
                translation += Vector3.up;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                translation += Vector3.down;
            }
            this.transform.position += translation * this.moveSpeed * Time.deltaTime;
        }
    }
}
