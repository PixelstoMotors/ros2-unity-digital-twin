using UnityEngine;
using System.IO;

public class RobotArmController : MonoBehaviour
{
    // ── フィールド ────────────────────────────────────────
    [Header("Joints")]
    public ArticulationBody[] targetJoints; // Element0=link1 〜 Element5=link6

    [Header("PickAndPlace")]
    public Transform  handlePoint;          // カップの把持点
    public GameObject placeTarget;          // 置き場所
    public GameObject cupObject;            // Inspector で coffeecup_3 を割り当てる
    private FixedJoint graspJoint;          // 把持中の FixedJoint 参照

    [Header("EndEffector")]
    public Transform endEffector;           // link6 配下の EndEffector

    public float graspThreshold = 0.05f;    // 到達判定距離 [m]
    public float moveSpeed = 20f;           // 未使用（CCD IK で直接制御）

    // ── 内部状態 ──────────────────────────────────────────
    private enum State { REACH_CUP, GRASP, REACH_PLACE, PLACE }
    private State currentState = State.REACH_CUP;
    private bool isCooldown = false;

    private const int CCD_ITERATIONS = 1;   // 1フレームあたりの反復回数

    private Vector3 cachedHandlePosition;
    private Vector3 cachedPlacePosition;
    private bool positionsCached = false;

    // ── CSV出力 ────────────────────────────────────────────
    private StreamWriter frameLogWriter;
    private StreamWriter eventLogWriter;
    private string logsDirectory;

    // ── Unity ライフサイクル ──────────────────────────────
    void Start()
    {
        if (targetJoints == null) return;

        // 全 joint の xDrive を初期化（damping は触らない）
        foreach (var joint in targetJoints)
        {
            if (joint == null) continue;
            var drive = joint.xDrive;
            drive.driveType  = ArticulationDriveType.Target;
            drive.forceLimit = 1000000f;
            // target は現在値を維持
            joint.xDrive = drive;
        }

        Debug.Log($"[START] endEffector={(endEffector == null ? "NULL" : endEffector.name)}");
        Debug.Log($"[START] handlePoint={(handlePoint == null ? "NULL" : handlePoint.name)}");
        Debug.Log($"[START] placeTarget={(placeTarget == null ? "NULL" : placeTarget.name)}");

        // アームとカップの衝突を無効化（把持前の弾き飛び防止）
        if (cupObject != null)
        {
            var cupColliders = cupObject.GetComponentsInChildren<Collider>();
            foreach (var joint in targetJoints)
            {
                if (joint == null) continue;
                var armColliders = joint.GetComponentsInChildren<Collider>();
                foreach (var armCol in armColliders)
                {
                    foreach (var cupCol in cupColliders)
                    {
                        Physics.IgnoreCollision(armCol, cupCol, true);
                    }
                }
            }
        }

        cachedHandlePosition = handlePoint.position;
        cachedPlacePosition = placeTarget.transform.position;
        positionsCached = true;

        // ── CSV出力の初期化 ────────────────────────────────
        logsDirectory = Path.Combine(Application.dataPath, "..", "Logs");
        Directory.CreateDirectory(logsDirectory);

        // フレームログCSVをオープン
        string frameLogPath = Path.Combine(logsDirectory, "robot_frame_log.csv");
        frameLogWriter = new StreamWriter(frameLogPath, false) { AutoFlush = true };
        frameLogWriter.WriteLine("time,state,dist,j0_pos,j1_pos,j2_pos,j3_pos,j4_pos,j5_pos,j0_vel,j1_vel,j2_vel,j3_vel,j4_vel,j5_vel,j0_force,j1_force,j2_force,j3_force,j4_force,j5_force,ee_x,ee_y,ee_z");

        // イベントログCSVをオープン
        string eventLogPath = Path.Combine(logsDirectory, "robot_event_log.csv");
        eventLogWriter = new StreamWriter(eventLogPath, false) { AutoFlush = true };
        eventLogWriter.WriteLine("time,event,dist,ee_x,ee_y,ee_z");

        Debug.Log($"[CSV] Frame log: {frameLogPath}");
        Debug.Log($"[CSV] Event log: {eventLogPath}");
    }

    void LateUpdate()
    {
        if (targetJoints == null || targetJoints.Length < 6) return;
        if (endEffector == null) return;

        // ── ターゲット座標を決定 ──────────────────────────
        Vector3 targetPosition = GetTargetPosition();

        // ── CCD IK（10回反復） ────────────────────────────
        // 関節制限値（mecharm_270_m5 URDFより）
        float[] lowerLimits = { -160f, -75f, -175f, -155f, -100f, -180f };
        float[] upperLimits = {  160f, 120f,   45f,  155f,  100f,  180f };

        for (int iter = 0; iter < CCD_ITERATIONS; iter++)
        {
            // link6(index5) → link1(index0) の順に処理
            for (int i = targetJoints.Length - 1; i >= 0; i--)
            {
                if (targetJoints[i] == null) continue;

                Transform jointTransform = targetJoints[i].transform;

                // ジョイントの真の回転軸をanchorRotationから取得
                Vector3 jointAxis = targetJoints[i].anchorRotation * Vector3.right;
                jointAxis = jointTransform.TransformDirection(jointAxis);

                // ローカル座標系でベクトルを計算
                Vector3 toEffector = endEffector.position - jointTransform.position;
                Vector3 toTarget   = targetPosition - jointTransform.position;

                // jointAxisに垂直な平面に射影
                toEffector = Vector3.ProjectOnPlane(toEffector, jointAxis).normalized;
                toTarget   = Vector3.ProjectOnPlane(toTarget,   jointAxis).normalized;

                float angle = Vector3.SignedAngle(toEffector, toTarget, jointAxis);

                // Debug可視化
                Debug.DrawRay(jointTransform.position, jointAxis * 0.1f, Color.red);
                Debug.DrawRay(jointTransform.position, toEffector * 0.1f, Color.green);
                Debug.DrawRay(jointTransform.position, toTarget   * 0.1f, Color.blue);

                var drive = targetJoints[i].xDrive;
                drive.forceLimit = 1000000f;
                drive.target    += angle * 0.05f;
                drive.target     = Mathf.Clamp(drive.target, lowerLimits[i], upperLimits[i]);
                targetJoints[i].xDrive = drive;
                targetJoints[i].SetDriveTarget(ArticulationDriveAxis.X, drive.target);
            }
        }

        // ── ステート遷移判定 ──────────────────────────────
        float dist = Vector3.Distance(endEffector.position, targetPosition);
        Debug.Log($"[IK] state={currentState} dist={dist:F3}");

        switch (currentState)
        {
            case State.REACH_CUP:
                if (dist <= graspThreshold && !isCooldown)
                {
                    isCooldown = true;
                    Invoke(nameof(ResetCooldown), 1.5f);
                    currentState = State.GRASP;
                    Debug.Log("[RobotArm] → GRASP");
                    WriteEventLog("TO_GRASP", dist);
                }
                break;

            case State.GRASP:
                // FixedJoint を動的生成して把持（距離 graspThreshold 以下で遷移済み）
                if (graspJoint == null && cupObject != null)
                {
                    var cup = cupObject;
                    graspJoint = cup.AddComponent<FixedJoint>();
                    graspJoint.connectedBody = endEffector.GetComponent<Rigidbody>();
                    graspJoint.breakForce  = Mathf.Infinity;
                    graspJoint.breakTorque = Mathf.Infinity;
                    Debug.Log("[RobotArm] GRASP: FixedJoint 生成完了");
                    WriteEventLog("FIXEDJOINT_CREATED", dist);
                    currentState = State.REACH_PLACE;
                    Debug.Log("[RobotArm] → REACH_PLACE");
                    WriteEventLog("TO_REACH_PLACE", dist);
                }
                break;

            case State.REACH_PLACE:
                if (dist <= graspThreshold && !isCooldown)
                {
                    isCooldown = true;
                    Invoke(nameof(ResetCooldown), 1.5f);
                    currentState = State.PLACE;
                    Debug.Log("[RobotArm] → PLACE");
                    WriteEventLog("TO_PLACE", dist);
                }
                break;

            case State.PLACE:
                // FixedJoint を破棄して設置
                if (graspJoint != null)
                {
                    // 解放前にカップの速度をゼロリセット（飛び散り防止）
                    var cupRb = cupObject.GetComponent<Rigidbody>();
                    if (cupRb != null)
                    {
                        cupRb.velocity = Vector3.zero;
                        cupRb.angularVelocity = Vector3.zero;
                    }
                    Destroy(graspJoint);
                    graspJoint = null;
                    Debug.Log("[RobotArm] PLACE: FixedJoint 破棄完了");
                    WriteEventLog("FIXEDJOINT_DESTROYED", dist);
                }
                isCooldown = true;
                Invoke(nameof(ResetCooldown), 2.0f);
                currentState = State.REACH_CUP;
                Debug.Log("[RobotArm] → REACH_CUP（ループ）");
                WriteEventLog("TO_REACH_CUP", dist);
                break;
        }

        // ── フレームログ出力 ──────────────────────────────
        if (frameLogWriter != null)
        {
            Vector3 ee = endEffector.position;
            string line = string.Format("{0:F4},{1},{2:F4}," +
                "{3:F4},{4:F4},{5:F4},{6:F4},{7:F4},{8:F4}," +
                "{9:F4},{10:F4},{11:F4},{12:F4},{13:F4},{14:F4}," +
                "{15:F4},{16:F4},{17:F4},{18:F4},{19:F4},{20:F4}," +
                "{21:F4},{22:F4},{23:F4}",
                Time.time, currentState.ToString(), dist,
                GetDOF(targetJoints[0].jointPosition), GetDOF(targetJoints[1].jointPosition),
                GetDOF(targetJoints[2].jointPosition), GetDOF(targetJoints[3].jointPosition),
                GetDOF(targetJoints[4].jointPosition), GetDOF(targetJoints[5].jointPosition),
                GetDOF(targetJoints[0].jointVelocity),  GetDOF(targetJoints[1].jointVelocity),
                GetDOF(targetJoints[2].jointVelocity),  GetDOF(targetJoints[3].jointVelocity),
                GetDOF(targetJoints[4].jointVelocity),  GetDOF(targetJoints[5].jointVelocity),
                GetDOF(targetJoints[0].jointForce),     GetDOF(targetJoints[1].jointForce),
                GetDOF(targetJoints[2].jointForce),     GetDOF(targetJoints[3].jointForce),
                GetDOF(targetJoints[4].jointForce),     GetDOF(targetJoints[5].jointForce),
                ee.x, ee.y, ee.z);
            frameLogWriter.WriteLine(line);
        }
    }

    private void WriteEventLog(string eventName, float dist)
    {
        if (eventLogWriter == null) return;
        Vector3 ee = endEffector != null ? endEffector.position : Vector3.zero;
        eventLogWriter.WriteLine($"{Time.time:F4},{eventName},{dist:F4},{ee.x:F4},{ee.y:F4},{ee.z:F4}");
    }

    private float GetDOF(ArticulationReducedSpace s) 
        => s.dofCount > 0 ? s[0] : 0f;

    void OnDestroy()
    {
        frameLogWriter?.Close();
        eventLogWriter?.Close();
    }

    private void ResetCooldown() { isCooldown = false; }

    // ── 現在ステートに応じたターゲット座標を返す ─────────
    private Vector3 GetTargetPosition()
    {
        switch (currentState)
        {
            case State.REACH_CUP:
            case State.GRASP:
                // handlePoint は Rigidbody で動くため毎フレーム取得
                return handlePoint != null ? handlePoint.position : transform.position;
            case State.REACH_PLACE:
            case State.PLACE:
                return cachedPlacePosition;
            default:
                return transform.position;
        }
    }
}
