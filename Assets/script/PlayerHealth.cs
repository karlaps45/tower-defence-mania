using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public TextMeshProUGUI hearttext;
    public TextMeshProUGUI cointext;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResourcesChanged += UpdateDisplay;
            UpdateDisplay(
                GameManager.Instance.PlayerHealth,
                GameManager.Instance.Coins
            );
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResourcesChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(int playerHealth, int coins)
    {
        if (hearttext != null)
        {
            hearttext.text = playerHealth.ToString();
        }

        if (cointext != null)
        {
            cointext.text = coins.ToString();
        }
    }
}
