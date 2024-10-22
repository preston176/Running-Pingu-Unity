using TMPro;
using UnityEngine;

public class UIGame : MonoBehaviour
{
    [Header("References")]
    public GameObject panel;
    public TMP_Text scoreText;
    public TMP_Text coinsText;
    public TMP_Text modifierText;

    private void Update()
    {
        var isPlaying = GameManager.instance.GameState == GameState.Playing;
        if (isPlaying)
        {
            scoreText.text = GameManager.instance.Score.ToString();
            coinsText.text = GameManager.instance.Coins.ToString();
            modifierText.text = $"x{GameManager.instance.ScoreModifier}";

            panel.SetActive(true);
        }
        else
        {
            panel.SetActive(false);
        }
    }
}