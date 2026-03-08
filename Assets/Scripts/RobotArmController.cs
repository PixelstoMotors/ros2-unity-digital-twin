using UnityEngine;

public class RobotArmController : MonoBehaviour
{
    public ArticulationBody targetJoint; 
    public GameObject targetKobachi;
    public float moveSpeed = 0.05f;

    void FixedUpdate()
    {
        if (targetJoint != null)
        {
            var drive = targetJoint.xDrive;
            drive.target += moveSpeed * Time.fixedDeltaTime;
            targetJoint.xDrive = drive;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("kobachi"))
        {
            Debug.Log("【成功】Contact with Kobachi detected!");
        }
    }
}