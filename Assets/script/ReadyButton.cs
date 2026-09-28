using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ReadyButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(StartEnemyRound);
    }

    private void Start()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.PhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(GameManager.Instance.CurrentPhase);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(StartEnemyRound);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PhaseChanged -= HandlePhaseChanged;
        }
    }

    private void StartEnemyRound()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentPhase == GamePhase.Buy)
        {
            GameManager.Instance.StartEnemyRound();
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        button.interactable = phase == GamePhase.Buy;
    }
}
