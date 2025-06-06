using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class FPSAgent : Agent
{
    public int teamId;
    public GameManager gameManager;

    private Transform head;
    private PlayerController playerController;
    private CharacterController controller;
    private Vector2 lastMousePosition;  // 上一帧鼠标位置
    private Transform nearestCrosshairEnemy = null;  // 离准心最近的敌人
    

    public override void Initialize()
    {
        head = transform.Find("head");
        playerController = GetComponent<PlayerController>();
        playerController.Initialize(gameManager, teamId);
        controller = GetComponent<CharacterController>();
        lastMousePosition = Input.mousePosition;
    }

    public override void OnEpisodeBegin()
    {
        // 重置玩家状态
        if (!playerController.IsAlive)
        {
            playerController.health = 100f;
            playerController.IsAlive = true;
            playerController.gameObject.SetActive(true);
            controller.enabled = true;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 查找离瞄准线（head的x正轴）夹角最小的敌人
        var rayDirection = head.forward;
        var enemies = teamId == 1 ?
            gameManager.team2Players :
            gameManager.team1Players;
        float minAngle = Mathf.Infinity;
        Vector3 enemyDirection = new();
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            // 计算离准星最近的敌人和瞄准线方向夹角
            enemyDirection = enemy.transform.position - head.position;
            float angle = Vector3.Angle(rayDirection, enemyDirection);
            if (angle < minAngle)
            {
                minAngle = angle;
                nearestCrosshairEnemy = enemy.transform;
            }
        }
        // 学习朝向关系
        sensor.AddObservation(enemyDirection);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 数组的值必须是自然数
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        Vector2 currentMousePosition = Input.mousePosition;
        Vector2 mouseDelta = currentMousePosition - lastMousePosition;
        lastMousePosition = currentMousePosition;

        // 旋转视角
        continuousActionsOut[0] = mouseDelta.x;
        continuousActionsOut[1] = mouseDelta.y;

        // 移动
        var input_h = Input.GetAxis("Horizontal");
        var input_v = Input.GetAxis("Vertical");

        if (input_h > 0)
        {
            discreteActionsOut[0] = 1;
        }
        if (input_h < 0)
        {
            discreteActionsOut[0] = 2;
        }
        if (input_v > 0)
        {
            discreteActionsOut[1] = 1;
        }
        if (input_v < 0)
        {
            discreteActionsOut[1] = 2;
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!playerController.IsAlive)
            return;

        // 1. Action -> Behavior
        float horizontal = actions.DiscreteActions[0] == 1? 1 : actions.DiscreteActions[0] == 2? -1 : 0;
        float vertical = actions.DiscreteActions[1] == 1? 1 : actions.DiscreteActions[1] == 2? -1 : 0;
        playerController.MoveXoZ(horizontal, vertical);

        // 手动操作才需要按射击键
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly && Input.GetButton("Fire1"))
        {
            playerController.Shoot();
        }
        // 旋转
        playerController.RotateVision(
            actions.ContinuousActions[0],
            actions.ContinuousActions[1]
        );

        // 准星尝试锁定离准心夹角最近的敌人
        playerController.TryLockOn(nearestCrosshairEnemy);
    }
}