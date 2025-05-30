using UnityEngine;
using Unity.MLAgents;

public class EnvironmentParameters : MonoBehaviour
{
    private void Start()
    {
        // 设置环境参数
        SetEnvironmentParameters();
    }

    private void SetEnvironmentParameters()
    {
        // 从ML-Agents环境参数获取配置
        var envParams = Academy.Instance.EnvironmentParameters;
        
        // 设置移动速度
        float moveSpeed = envParams.GetWithDefault("move_speed", 5f);
        foreach (var agent in FindObjectsOfType<FPSAgent>())
        {
            agent.GetComponent<PlayerController>().moveSpeed = moveSpeed;
        }
        
        // 设置武器伤害
        float damage = envParams.GetWithDefault("damage", 20f);
        foreach (var agent in FindObjectsOfType<FPSAgent>())
        {
            agent.GetComponent<PlayerController>().damage = damage;
        }
        
        // 设置生命值
        float health = envParams.GetWithDefault("health", 100f);
        foreach (var agent in FindObjectsOfType<FPSAgent>())
        {
            agent.GetComponent<PlayerController>().health = health;
        }
    }
}