using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public TextMeshProUGUI totalScoreText;

    public int TotalScore { get; private set; }

    void Awake()
    {
        if (Instance && Instance != this)
        { Destroy(gameObject); return; }

        Instance = this;
        
        if (totalScoreText) 
        totalScoreText.text = "Scor: 0";
    }

    public void AddScore(int points, Vector3 _)
    {
        TotalScore += points;
        if (totalScoreText) totalScoreText.text = $"Scor: {TotalScore}";
        
    }
}