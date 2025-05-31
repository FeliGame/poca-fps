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
    public float matchTime = 3 * 60; // 单位：秒
    public Transform team1SpawnCircle;
    public Transform team2SpawnCircle;
    public float spawnCircleRadius = 5f;

    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject bulletHolePrefab;

    private List<PlayerController> team1Players = new();
    private List<PlayerController> team2Players = new();
    private float currentTime;
    private bool isGameActive = false;

    private SimpleMultiAgentGroup m_Team1Group;
    private SimpleMultiAgentGroup m_Team2Group;

    private int m_ResetTimer;


    private void Start()
    {
        // 初始化团队组
        m_Team1Group = new SimpleMultiAgentGroup();
        m_Team2Group = new SimpleMultiAgentGroup();

        InitializeGame();
        m_ResetTimer = 0;
    }

    private void FixedUpdate()
    {
        if (isGameActive)
        {
            currentTime -= Time.fixedDeltaTime;
            m_ResetTimer += 1;
            if (currentTime <= 0)
            {
                EndGame();
                RestartGame();
            }

            // 检查游戏是否结束（一方全灭）
            if (CheckTeamEliminated(team1Players))
            {
                TeamWin(2);
                RestartGame();
            }
            else if (CheckTeamEliminated(team2Players))
            {
                TeamWin(1);
                RestartGame();
            }
        }
    }

    private void InitializeGame()
    {
        // 创建团队 1
        for (int i = 0; i < teamSize; i++)
        {
            GameObject playerObj = Instantiate(playerPrefab, GetRandomSpawnPosition(team1SpawnCircle), Quaternion.identity, transform);
            playerObj.name = "Team1_P" + (i + 1);
            playerObj.GetComponent<BehaviorParameters>().TeamId = 0;
            FPSAgent agent = playerObj.GetComponent<FPSAgent>();
            agent.gameManager = this;
            agent.teamId = 1;
            agent.Initialize();
            m_Team1Group.RegisterAgent(agent);
            team1Players.Add(playerObj.GetComponent<PlayerController>());
        }

        // 创建团队 2
        for (int i = 0; i < teamSize; i++)
        {
            GameObject playerObj = Instantiate(playerPrefab, GetRandomSpawnPosition(team2SpawnCircle), Quaternion.identity, transform);
            playerObj.name = "Team2_P" + (i + 1);
            playerObj.GetComponent<BehaviorParameters>().TeamId = 1;
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

    private Vector3 GetRandomSpawnPosition(Transform spawnCircle)
    {
        Vector2 random2D = Random.insideUnitCircle * spawnCircleRadius;
        Vector3 random3D = new(random2D.x, 0f, random2D.y);
        return spawnCircle.position + random3D;
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

    private void TeamWin(int winningTeamId)
    {
        Debug.Log("Team " + winningTeamId + " Wins!");
        if (winningTeamId == 1)
        {
            m_Team1Group.AddGroupReward(1 - (float)m_ResetTimer / (int)(matchTime / Time.fixedDeltaTime));
            m_Team2Group.AddGroupReward(-1);
        }
        else
        {
            m_Team2Group.AddGroupReward(1 - (float)m_ResetTimer / (int)(matchTime / Time.fixedDeltaTime));
            m_Team1Group.AddGroupReward(-1);
        }
    }

    private void EndGame()
    {
        isGameActive = false;

        // 根据剩余玩家数量判断胜负
        int team1AliveCount = GetAlivePlayerCount(team1Players);
        int team2AliveCount = GetAlivePlayerCount(team2Players);

        if (team1AliveCount > team2AliveCount)
        {
            TeamWin(1);
        }
        else if (team2AliveCount > team1AliveCount)
        {
            TeamWin(2);
        }
        else
        {
            Debug.Log("It's a tie!");
        }

        m_Team1Group.EndGroupEpisode();
        m_Team2Group.EndGroupEpisode();
    }

    private int GetAlivePlayerCount(List<PlayerController> team)
    {
        int aliveCount = 0;
        foreach (var player in team)
        {
            if (player.IsAlive)
            {
                aliveCount++;
            }
        }
        return aliveCount;
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}