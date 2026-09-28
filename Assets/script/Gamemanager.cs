using System;
using UnityEngine;

public enum GamePhase
{
    Buy,
    EnemyRound,
    BossRound,
    Victory,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [field: SerializeField]
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Buy;

    public event Action<GamePhase> PhaseChanged;
    public event Action<int, int> ResourcesChanged;

    [Header("Player resources")]
    [SerializeField] private int startingPlayerHealth = 10;
    [SerializeField] private int startingCoins = 50;

    public int PlayerHealth { get; private set; }
    public int Coins { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PlayerHealth = startingPlayerHealth;
        Coins = startingCoins;

        // Keep the manager when changing scenes.
        DontDestroyOnLoad(gameObject);
    }

    public void SetPhase(GamePhase newPhase)
    {
        if (CurrentPhase == newPhase)
            return;

        CurrentPhase = newPhase;

        Debug.Log($"Game phase changed to: {CurrentPhase}");

        PhaseChanged?.Invoke(CurrentPhase);
    }

    public void StartBuyPhase()
    {
        SetPhase(GamePhase.Buy);
    }

    public void StartEnemyRound()
    {
        SetPhase(GamePhase.EnemyRound);
    }

    public void StartBossRound()
    {
        SetPhase(GamePhase.BossRound);
    }

    public void DamagePlayer(int amount)
    {
        if (amount <= 0 || CurrentPhase == GamePhase.GameOver)
            return;

        PlayerHealth = Mathf.Max(0, PlayerHealth - amount);
        ResourcesChanged?.Invoke(PlayerHealth, Coins);

        if (PlayerHealth <= 0)
        {
            SetPhase(GamePhase.GameOver);
        }
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        Coins += amount;
        ResourcesChanged?.Invoke(PlayerHealth, Coins);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount < 0 || Coins < amount)
            return false;

        Coins -= amount;
        ResourcesChanged?.Invoke(PlayerHealth, Coins);
        return true;
    }
}
