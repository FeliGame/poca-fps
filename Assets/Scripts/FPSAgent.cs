using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class FPSAgent : Agent
{
    public int teamId;
    public GameManager gameManager;

    private PlayerController playerController;
    private CharacterController controller;
    private Vector2 lastMousePosition;  // 上一帧鼠标位置
    

    public override void Initialize()
    {
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

        // 射击
        discreteActionsOut[2] = Input.GetButton("Fire1") ? 1 : 0;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!playerController.IsAlive)
            return;

        // 1. Action -> Behavior
        float horizontal = actions.DiscreteActions[0] == 1? 1 : actions.DiscreteActions[0] == 2? -1 : 0;
        float vertical = actions.DiscreteActions[1] == 1? 1 : actions.DiscreteActions[1] == 2? -1 : 0;
        playerController.MoveXoZ(horizontal, vertical);

        int shootAction = actions.DiscreteActions[2];
        if (shootAction == 1)
        {
            playerController.Shoot();
        }

        // 旋转
        playerController.RotateVision(
            actions.ContinuousActions[0],
            actions.ContinuousActions[1]
        );

        // AddReward(-0.001f);  // 每帧小惩罚以鼓励效率
    }
}