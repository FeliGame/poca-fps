using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Basic")]
    public float health = 100f;
    public bool IsAlive = true;

    private int teamId;

    [Header("Control")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.02f;
    public float gravity = -9.81f;
    public Transform head;

    private float jumpForce = 20f;

    [Header("Weapon")]
    public float damage = 20f;
    public float fireInterval = 0.1f;
    public float range = 100f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;  // Jump时不能第一时间读取CharacterController的isGrounded，因此用该变量缓存
    private float fireCooldown;
    private float currentPitch = 0f;    // 当前俯仰角
    private float rayOriginOffset = 1f;  // 射线发射点偏移（防止撞到自己）


    public void Initialize(int teamId)
    {
        this.teamId = teamId;
        controller = GetComponent<CharacterController>();
        // 设置玩家颜色
        SetTeamColor();
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

    private void Update()
    {
        if (!IsAlive)
            return;

        // 速度负值固定
        if (!controller.isGrounded && velocity.y < 0)
            velocity.y = -4f;

        // 应用重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        isGrounded = controller.isGrounded;

        // 射击冷却
        if (fireCooldown > 0)
            fireCooldown -= Time.deltaTime;
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
        controller.Move(moveSpeed * Time.deltaTime * moveDirection);
    }

    public void Jump()
    {
        if (isGrounded && IsAlive)
        {
            Debug.Log("Jumping force..." + jumpForce);
            velocity.y = Mathf.Sqrt(jumpForce);
        }
    }

    // 旋转视角，输入值为视角变化量
    public void RotateVision(float rotation_h, float rotation_v)
    {
        if (!IsAlive) return;

        float rotationFactor = 180f * mouseSensitivity * Time.deltaTime;
        float horizontalRotation = rotation_h * rotationFactor;
        float verticalRotation = rotation_v * rotationFactor;
        // 水平旋转（身体转向）
        transform.Rotate(Vector3.up, horizontalRotation);

        // 垂直旋转（只动头）
        currentPitch -= verticalRotation;
        // 限制俯仰角在 [-90, 90] 之间
        currentPitch = Mathf.Clamp(currentPitch, -90f, 90f);
        // 应用限制后的俯仰角
        head.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    public void Shoot()
    {
        if (fireCooldown > 0 || !IsAlive) return;

        Vector3 rayDirection = head.forward;
        Vector3 rayOrigin = head.position + rayDirection * rayOriginOffset;

        RaycastHit hit;
        // 仅获取最近击中物体
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, range))
        {
            Debug.Log("Hitting " + hit.transform.name);
            // 绘制射线
            float rayDuration = 0.5f;
            Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.red, rayDuration);

            // 检查是否击中敌方队伍的Player
            PlayerController target = hit.transform.GetComponent<PlayerController>();
            if (target != null && target.teamId != teamId)
            {
                target.TakeDamage(damage);
            }
        }

        fireCooldown = fireInterval;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log("Player taking damage: " + amount + ", HP left: " + health);
        if (health <= 0 && IsAlive)
        {
            Die();
        }
    }

    private void Die()
    {
        IsAlive = false;

        // 禁用控制器
        controller.enabled = false;
        gameObject.SetActive(false);

        // 重生逻辑
        StartCoroutine(RespawnAfterDelay(3f));
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 重置属性
        health = 100f;
        IsAlive = true;

        // 选择重生点
        Transform spawnCircle = teamId == 1 ?
            GameManager.Instance.team1SpawnCircle :
            GameManager.Instance.team2SpawnCircle;

        Vector2 random2D = Random.insideUnitCircle * GameManager.Instance.spawnCircleRadius;
        Vector3 random3D = new(random2D.x, 0f, random2D.y);
        Vector3 spawnPos = spawnCircle.position + random3D;

        // 重置位置和状态
        transform.SetPositionAndRotation(spawnPos, spawnCircle.rotation);
        controller.enabled = true;
        gameObject.SetActive(true);
    }
}