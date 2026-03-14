using UnityEngine;

/// <summary>
/// PickAndPlace ステートマシン
/// State: APPROACH → GRASP → PLACE
/// Robot_0_0 に AddComponent して使用する
/// </summary>
public class PickAndPlaceController : MonoBehaviour
{
    [Header("References")]
    public GameObject robotArm;      // Robot_0_0
    public GameObject graspTarget;   // coffeecup_3 (Rigidbody 付き)
    public Transform  handlePoint;   // coffeecup_3/HandlePoint
    public GameObject placeTarget;   // PlaceTarget

    [Header("Settings")]
    public float graspThreshold = 0.05f;  // 把持判定距離 [m]
    public float driveForceLimit = 1000f; // xDrive forceLimit

    // ── 内部状態 ──────────────────────────────────────────
    private ArticulationBody endEffector;
    private FixedJoint       fixedJoint;

    private enum State { APPROACH, GRASP, PLACE }
    private State currentState = State.APPROACH;

    // ── Unity ライフサイクル ──────────────────────────────
    private void Start()
    {
        endEffector = GetNearestArticulationBody(robotArm);
        if (endEffector == null)
            Debug.LogError("[PickAndPlace] endEffector が見つかりません");
    }

    private void Update()
    {
        if (endEffector == null) return;

        switch (currentState)
        {
            case State.APPROACH: UpdateApproach(); break;
            case State.GRASP:    UpdateGrasp();    break;
            case State.PLACE:    UpdatePlace();    break;
        }
    }

    // ── APPROACH ─────────────────────────────────────────
    private void UpdateApproach()
    {
        Vector3 dir = handlePoint.position - endEffector.transform.position;

        SetDriveTarget(endEffector, dir);

        if (dir.magnitude <= graspThreshold)
        {
            currentState = State.GRASP;
            Debug.Log("[PickAndPlace] → GRASP");
        }
    }

    // ── GRASP ─────────────────────────────────────────────
    private void UpdateGrasp()
    {
        // FixedJoint を coffeecup_3 に生成し、エンドエフェクタの Rigidbody に接続
        fixedJoint = graspTarget.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = endEffector.GetComponent<Rigidbody>();

        currentState = State.PLACE;
        Debug.Log("[PickAndPlace] → PLACE");
    }

    // ── PLACE ─────────────────────────────────────────────
    private void UpdatePlace()
    {
        Vector3 dir = placeTarget.transform.position - endEffector.transform.position;

        SetDriveTarget(endEffector, dir);

        // PlaceTarget に十分近づいたら設置完了
        if (dir.magnitude <= graspThreshold)
        {
            if (fixedJoint != null)
            {
                Destroy(fixedJoint);
                fixedJoint = null;
            }
            currentState = State.APPROACH;
            Debug.Log("[PickAndPlace] → APPROACH (placed)");
        }
    }

    // ── ヘルパー ──────────────────────────────────────────

    /// <summary>
    /// ArticulationBody の xDrive.target / forceLimit を設定する。
    /// Stiffness / Damping は一切変更しない。
    /// </summary>
    private void SetDriveTarget(ArticulationBody body, Vector3 direction)
    {
        var xDrive = body.xDrive;
        xDrive.target     = Mathf.Clamp(direction.x, -1f, 1f);
        xDrive.forceLimit = driveForceLimit;
        body.xDrive = xDrive;

        var yDrive = body.yDrive;
        yDrive.target     = Mathf.Clamp(direction.y, -1f, 1f);
        yDrive.forceLimit = driveForceLimit;
        body.yDrive = yDrive;

        var zDrive = body.zDrive;
        zDrive.target     = Mathf.Clamp(direction.z, -1f, 1f);
        zDrive.forceLimit = driveForceLimit;
        body.zDrive = zDrive;
    }

    /// <summary>
    /// obj 配下の全 ArticulationBody から handlePoint に最も近いものを返す。
    /// </summary>
    private ArticulationBody GetNearestArticulationBody(GameObject obj)
    {
        if (obj == null) return null;

        ArticulationBody[] bodies = obj.GetComponentsInChildren<ArticulationBody>();
        ArticulationBody nearest  = null;
        float minDist = float.MaxValue;

        foreach (ArticulationBody body in bodies)
        {
            float dist = Vector3.Distance(body.transform.position, handlePoint.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = body;
            }
        }

        return nearest;
    }
}
