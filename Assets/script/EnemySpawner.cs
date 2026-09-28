
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    [FormerlySerializedAs("EnemyPrefabs")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [FormerlySerializedAs("spawnpoint")]
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float timeBetweenEnemies = 1f;

    [Header("Round scaling")]
    [SerializeField] private int startingEnemyCount = 3;
    [SerializeField] private int extraEnemiesPerRound = 2;
    [SerializeField] private float extraSpeedPerRound = 0.1f;
    [SerializeField] private float extraHealthPerRound = 0.25f;

    [SerializeField] private List<GameObject> enemiesActive = new();
    private int enemyRoundNumber;

    private void Start()
    {
        GameManager.Instance.PhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(GameManager.Instance.CurrentPhase);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(GamePhase newPhase)
    {
        if (newPhase == GamePhase.EnemyRound)
        {
            enemyRoundNumber++;
            StartCoroutine(SpawnEnemyWave());
        }
        else if (newPhase == GamePhase.BossRound)
        {
            SpawnBoss();
        }
    }

    private IEnumerator SpawnEnemyWave()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 ||
            enemyPrefabs[0] == null)
        {
            Debug.LogWarning("EnemySpawner needs an enemy prefab at index 0.");
            yield break;
        }

        int enemyCount = startingEnemyCount
            + (enemyRoundNumber - 1) * extraEnemiesPerRound;

        for (int i = 0; i < enemyCount; i++)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.EnemyRound)
                yield break;

            SpawnEnemy(enemyPrefabs[0]);
            yield return new WaitForSeconds(timeBetweenEnemies);
        }

        while (GameManager.Instance.CurrentPhase == GamePhase.EnemyRound)
        {
            enemiesActive.RemoveAll(enemy => enemy == null);

            if (enemiesActive.Count == 0)
            {
                GameManager.Instance.StartBuyPhase();
                yield break;
            }

            yield return null;
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        GameObject newEnemyObject = Instantiate(
            prefab,
            spawnPoint.transform.position,
            spawnPoint.transform.rotation
        );

        int completedScalingSteps = Mathf.Max(0, enemyRoundNumber - 1);
        float speedMultiplier = 1f
            + completedScalingSteps * extraSpeedPerRound;
        float healthMultiplier = 1f
            + completedScalingSteps * extraHealthPerRound;

        Enemy newEnemy = newEnemyObject.GetComponent<Enemy>();

        if (newEnemy != null)
        {
            newEnemy.Initialize(
                waypoints,
                speedMultiplier,
                healthMultiplier
            );
        }

        enemiesActive.Add(newEnemyObject);
    }

    private void SpawnBoss()
    {
        if (enemyPrefabs.Length <= 2)
            return;

        SpawnEnemy(enemyPrefabs[2]);
    }
}
