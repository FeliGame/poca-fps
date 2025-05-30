using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int teamSize = 5;
    public float matchTime = 5 * 60; // 5分钟
    public Transform team1SpawnCircle;
    public Transform team2SpawnCircle;
    public float spawnCircleRadius = 5f;
    public GameObject playerPrefab;

    private List<PlayerController> team1Players = new();
    private List<PlayerController> team2Players = new();
    private float currentTime;
    private bool isGameActive = false;
    private bool isMouseVisible;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        if (isGameActive)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
                EndGame();

            // 检查游戏是否结束（一方全灭）
            if (CheckTeamEliminated(team1Players))
            {
                Debug.Log("Team 2 Wins!");
                EndGame();
            }
            else if (CheckTeamEliminated(team2Players))
            {
                Debug.Log("Team 1 Wins!");
                EndGame();
            }
        }

        // 重新开始游戏
        if (Input.GetKeyDown(KeyCode.R))
            RestartGame();

        // 鼠标锁定
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleMouseVisible(!isMouseVisible);
        if (Input.GetMouseButtonDown(0))
            ToggleMouseVisible(false);
    }

    private void InitializeGame()
    {
        currentTime = matchTime;
        isGameActive = true;
        ToggleMouseVisible(false);

        SpawnPlayers();
    }

    private void ToggleMouseVisible(bool isVisible)
    {
        isMouseVisible = isVisible;
        Cursor.visible = isVisible;
    }

    private void SpawnPlayers()
    {
        // 生成玩家
        SpawnTeam(team1Players, team1SpawnCircle, 1);
        SpawnTeam(team2Players, team2SpawnCircle, 2);
    }

    private void SpawnTeam(List<PlayerController> team, Transform spawnCircle, int teamId)
    {
        for (int i = 0; i < teamSize; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * spawnCircleRadius;
            Vector3 random3D = new (random2D.x, 0f, random2D.y);
            Vector3 spawnPos = spawnCircle.position + random3D;
            GameObject playerObj = Instantiate(playerPrefab, spawnPos, spawnCircle.rotation, spawnCircle);

            // Player的行为由Agent先行控制，因此应该调用Agent的初始化函数
            FPSAgent agent = playerObj.GetComponent<FPSAgent>();
            agent.teamId = teamId;
            agent.Initialize();

            team.Add(playerObj.GetComponent<PlayerController>());
        }
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

    private void EndGame()
    {
        isGameActive = false;
        // 显示游戏结束UI
        Debug.Log("Game Over!");
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}