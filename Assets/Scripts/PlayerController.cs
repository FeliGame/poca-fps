using UnityEngine;
using System.Collections;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public class PlayerController : MonoBehaviour
{
    [Header("Basic")]
    public float health = 100f;
    public bool IsAlive = true;

    private int teamId;

    [Header("Control")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.02f;
    private float mouseRotationFactor;
    public float gravity = -9.81f;
    public Transform head;

    [Header("Weapon")]
    public float damage = 50f;
    public float fireInterval = 0.1f;
    // 随机弹道
    public float fireRecover = 0.25f;   // 弹道恢复的时间
    public float stationaryJitter = 2f; // 站定状态下的随机抖动角度范围
    public float movingJitter = 5f; // 移动或在空中状态下的随机抖动角度范围

    private float range = 1000f;
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 rayDirection, rayOrigin;
    private bool isGrounded;  // Jump时不能第一时间读取CharacterController的isGrounded，因此用该变量缓存
    private float fireCooldown;
    private float fireRecoverCooldown;
    private float currentPitch = 0f;    // 当前俯仰角
    private float rayOriginOffset = 0.5f;  // 射线发射点偏移（防止撞到自己）
    private Vector3 lastPosition; // 上一帧Player位置
    private GameManager gameManager;
    private FPSAgent agent;


    public void Initialize(GameManager gameManager, int teamId)
    {
        this.teamId = teamId;
        controller = GetComponent<CharacterController>();
        // 设置玩家颜色
        SetTeamColor();
        this.gameManager = gameManager;
        agent = GetComponent<FPSAgent>();
        mouseRotationFactor = 180f * mouseSensitivity;
    }

    private void SetTeamColor()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Color teamColor = teamId == 1 ? Color.blue : Color.red;

        foreach (var renderer in renderers)
        {
            renderer.material.color = teamColor;
        }
    }

    private void FixedUpdate()
    {
        if (!IsAlive)
            return;

        // 绘制瞄准线
        Debug.DrawRay(rayOrigin, rayDirection, Color.green, 0f);

        // 速度负值固定
        if (!controller.isGrounded && velocity.y < 0)
            velocity.y = -4f;

        // 应用重力
        velocity.y += gravity * Time.fixedDeltaTime;
        controller.Move(velocity * Time.fixedDeltaTime);

        isGrounded = controller.isGrounded;

        // 射击冷却
        if (fireCooldown > 0)
            fireCooldown -= Time.fixedDeltaTime;
        // 弹道恢复
        if (fireRecoverCooldown > 0)
            fireRecoverCooldown -= Time.fixedDeltaTime;

        // AI自动射击
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.HeuristicOnly)
            Shoot();

        lastPosition = transform.position;
    }

    // 基于头部方向在 XoZ 平面移动
    public void MoveXoZ(float horizontal, float vertical)
    {
        if (!IsAlive) return;

        // 计算基于头部方向的移动向量，忽略 Y 轴分量
        Vector3 forward = head.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = head.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDirection = right * horizontal + forward * vertical;
        moveDirection.Normalize();

        // 移动
        controller.Move(moveSpeed * Time.fixedDeltaTime * moveDirection);
    }

    // 旋转视角，输入值为鼠标位移（isMouse=true），否则为视角变化量
    public void RotateVision(float rotation_h, float rotation_v, bool isMouseInput)
    {
        if (!IsAlive) return;
        float horizontalRotation = rotation_h * Time.fixedDeltaTime * (isMouseInput ? mouseRotationFactor:1);
        float verticalRotation = rotation_v * Time.fixedDeltaTime * (isMouseInput ? mouseRotationFactor:1);
        // 水平旋转（身体转向）
        transform.Rotate(Vector3.up, horizontalRotation);

        // 垂直旋转（只动头）
        currentPitch -= verticalRotation;
        // 限制俯仰角在 [-90, 90] 之间
        currentPitch = Mathf.Clamp(currentPitch, -90f, 90f);
        // 应用限制后的俯仰角
        head.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);

        // 更新瞄准线方向
        rayDirection = head.forward;
        rayOrigin = head.position + rayDirection * rayOriginOffset;
    }

    // 如果敌人在视野范围内，准心【直接】锁定敌人
    public void TryLockOn(Transform enemy)
    {
        if (!IsAlive || enemy == null) return;
        // 从当前Transform投射ray到enemy，如果ray能直接命中enemy，则先沿y轴转动整个transform（水平转身），然后沿x轴转动head（上下抬头），直到ray能直接命中enemy
        Vector3 enemyDir = enemy.position - head.position;
        // 如果敌人和头部方向夹角，在RayPerceptionSensor的MaxRayDegrees外，则不锁定
        float angle = Vector3.Angle(head.forward, enemyDir);
        if (angle > GetComponent<RayPerceptionSensorComponent3D>().MaxRayDegrees)
            return;
        if (Physics.Raycast(head.position, enemyDir, out RaycastHit hit, range))
        {
            if (hit.transform == enemy)
            {
                // 水平旋转身体（身体转向）
                Vector3 horizontalDir = enemyDir;
                horizontalDir.y = 0f;
                if (horizontalDir.magnitude > 0)
                {
                    horizontalDir.Normalize();
                    Quaternion horizontalRotation = Quaternion.LookRotation(horizontalDir, Vector3.up);
                    transform.rotation = horizontalRotation;
                }

                // 垂直旋转头部（只动头）
                float pitch = Vector3.SignedAngle(head.parent.forward, enemyDir, head.right);
                pitch = Mathf.Clamp(pitch, -90f, 90f);
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);

                // 更新瞄准线方向和发射点
                rayDirection = head.forward;
                rayOrigin = head.position + rayDirection * rayOriginOffset;
            }
        }
    }

    public void Shoot()
    {
        if (fireCooldown > 0 || !IsAlive) return;

        // 计算随机抖动范围
        float jitterRange;
        bool isMoving = (transform.position != lastPosition) || !isGrounded;
        if (isMoving)
        {
            jitterRange = movingJitter;
            // agent.AddReward(-0.01f);
        }
        else
        {
            if (fireRecoverCooldown <= 0)
            {
                jitterRange = 0f; // 站定第一次射击准确
            }
            else
            {
                jitterRange = stationaryJitter; // 站定后续射击抖动
                // agent.AddReward(-0.01f);
            }
        }

        // 仅获取最近击中物体
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, range))
        {
            // 检查是否击中敌方队伍的Player
            PlayerController target = hit.transform.GetComponent<PlayerController>();
            if (target != null && target.teamId != teamId)
            {
                // 命中线
                Debug.DrawRay(rayOrigin, rayDirection * hit.distance, teamId == 1 ? Color.blue : Color.red, 0.5f);
                target.TakeDamage(transform, damage);
                // AI仅命中时后坐力（此处假设瞄准到就会开枪，这导致AI的第一发子弹一定准确）
                if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.HeuristicOnly)
                    HandleJitter(jitterRange);
            }
            else  // 击中非Player对象，生成弹孔
            {
                // GameObject bulletHole = Instantiate(gameManager.bulletHolePrefab, hit.point, Quaternion.LookRotation(-hit.normal));
                // bulletHole.GetComponent<Renderer>().material.color = Color.black;
                // bulletHole.transform.position -= bulletHole.transform.forward * 0.01f;
                // Destroy(bulletHole, 2f);
            }
            // 人只要开枪就有后坐力
            if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly)
                HandleJitter(jitterRange);
        }
    }

    private void HandleJitter(float jitterRange)
    {
        // 击中后后坐力：随机抖动视角
        if (jitterRange > 0)
        {
            float jitterX = Random.Range(-jitterRange, jitterRange);
            float jitterY = Random.Range(-jitterRange, jitterRange);
            RotateVision(jitterX, jitterY, false);
        }

        fireCooldown = fireInterval;
        fireRecoverCooldown = fireRecover;
    }

    public void TakeDamage(Transform from, float amount)
    {
        FPSAgent killerAgent = from.GetComponent<FPSAgent>();
        // 个人A
        killerAgent.AddReward(0.5f);

        health -= amount;
        if (health <= 0 && IsAlive)
        {
            Debug.Log(from.gameObject.name + " killed " + gameObject.name);
            // 个人K，不怕死
            killerAgent.AddReward(1f);
            // agent.AddReward(-1f);
            // 团队K
            var manager = killerAgent.gameManager;
            var score = manager.currentTime / manager.matchTime;  // 击杀效率
            if (killerAgent.teamId == 1)
            {
                manager.m_Team1Group.AddGroupReward(score);
                manager.m_Team2Group.AddGroupReward(-1f);
            }
            else
            {
                manager.m_Team1Group.AddGroupReward(-1f);
                manager.m_Team2Group.AddGroupReward(score);
            }
            Die();
        }
    }

    private void Die()
    {
        IsAlive = false;

        // 禁用控制器
        controller.enabled = false;

        // 延迟重生
        gameObject.SetActive(false);
        // Invoke(nameof(Respawn), 1f);
    }

    public void Respawn()
    {
        // 先禁用，随后的更新位置才会生效
        gameObject.SetActive(false);
        // 重置属性
        health = 100f;
        IsAlive = true;

        // 选择重生点
        Transform spawnCircle = teamId == 1 ?
            gameManager.team1SpawnCircle :
            gameManager.team2SpawnCircle;

        // 重置位置和状态
        transform.SetPositionAndRotation(
            gameManager.GetRandomSpawnPosition(spawnCircle),
            gameManager.GetRandomSpawnYRotation());
        controller.enabled = true;
        gameObject.SetActive(true);
    }
}