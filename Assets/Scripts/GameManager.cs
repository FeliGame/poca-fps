using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int teamSize = 5;
    public float matchTime = 60; // 单位：秒
    public float currentTime;
    public Transform team1SpawnCircle;
    public Transform team2SpawnCircle;
    public float spawnCircleRadius = 5f;

    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject bulletHolePrefab;

    public List<PlayerController> team1Players = new();
    public List<PlayerController> team2Players = new();
    private bool isGameActive = false;

    public SimpleMultiAgentGroup m_Team1Group;
    public SimpleMultiAgentGroup m_Team2Group;


    private void Start()
    {
        // 初始化团队组
        m_Team1Group = new SimpleMultiAgentGroup();
        m_Team2Group = new SimpleMultiAgentGroup();

        InitializeGame();
    }

    private void FixedUpdate()
    {
        if (isGameActive)
        {
            currentTime -= Time.fixedDeltaTime;
            if (currentTime <= 0)
            {
                Debug.Log("Timeout");
                RestartGame();
            }
            // 检查游戏是否结束（一方全灭）
            if (CheckTeamEliminated(team1Players))
            {
                Debug.Log("Team2 Win!");
                RestartGame();
            }
            else if (CheckTeamEliminated(team2Players))
            {
                Debug.Log("Team1 Win!");
                RestartGame();
            }
        }
    }

    private void InitializeGame()
    {
        // 创建团队 1
        int i = team1Players.Count;
        for (; i < teamSize; i++)
        {
            GameObject playerObj = Instantiate(playerPrefab, GetRandomSpawnPosition(team1SpawnCircle), GetRandomSpawnYRotation(), transform);
            playerObj.name = "Team1_P" + (i + 1);
            playerObj.GetComponent<BehaviorParameters>().TeamId = 0;
            playerObj.tag = "blueAgent";
            FPSAgent agent = playerObj.GetComponent<FPSAgent>();
            agent.gameManager = this;
            agent.teamId = 1;
            agent.Initialize();
            m_Team1Group.RegisterAgent(agent);
            team1Players.Add(playerObj.GetComponent<PlayerController>());
        }

        // 创建团队 2
        i = team2Players.Count;
        for (; i < teamSize; i++)
        {
            GameObject playerObj = Instantiate(playerPrefab, GetRandomSpawnPosition(team2SpawnCircle), GetRandomSpawnYRotation(), transform);
            playerObj.name = "Team2_P" + (i + 1);
            playerObj.GetComponent<BehaviorParameters>().TeamId = 1;
            playerObj.tag = "redAgent";
            FPSAgent agent = playerObj.GetComponent<FPSAgent>();
            agent.gameManager = this;
            agent.teamId = 2;
            agent.Initialize();
            m_Team2Group.RegisterAgent(agent);
            team2Players.Add(playerObj.GetComponent<PlayerController>());
        }

        currentTime = matchTime;
        isGameActive = true;
    }

    private bool CheckTeamEliminated(List<PlayerController> team)
    {
        foreach (var player in team)
        {
            if (player.IsAlive)
                return false;
        }
        return true;
    }

    public Vector3 GetRandomSpawnPosition(Transform spawnCircle)
    {
        Vector2 random2D = Random.insideUnitCircle * spawnCircleRadius;
        Vector3 random3D = new(random2D.x, 0f, random2D.y);
        return spawnCircle.position + random3D;
    }

    public Quaternion GetRandomSpawnYRotation()
    {
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    public void RestartGame()
    {
        Debug.Log("Restart new game...");
        m_Team1Group.EndGroupEpisode();
        m_Team2Group.EndGroupEpisode();

        isGameActive = true;
        currentTime = matchTime;

        foreach (var player in team1Players)
        {
            player.Respawn();
        }
        foreach (var player in team2Players)
        {
            player.Respawn();
        }
    }
}