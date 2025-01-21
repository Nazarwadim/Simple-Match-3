using TMPro;
using UnityEngine;

public sealed class ScoreCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _scoreText;

    private int _score;

    public int Score
    {
        get => _score;
        set
        {
            if (_score != value)
            {
                _score = value;
                _scoreText.SetText($"Score: {value}");
            }
        }
    }
}
