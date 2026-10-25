using TMPro;
using UnityEngine;

// Screen-space UI: home (sport/mode select), HUD (stage, score, hints), results.
public class UIController : MonoBehaviour
{
    public GameObject homePanel, hudPanel, resultPanel;
    public TMP_Text stageText, scoreText, countText, hintText, feedbackText, resultText;

    public void ShowHome()    { homePanel.SetActive(true);  hudPanel.SetActive(false); resultPanel.SetActive(false); }
    public void ShowHUD()     { homePanel.SetActive(false); hudPanel.SetActive(true);  resultPanel.SetActive(false); }
    public void ShowResults(string summary)
    {
        homePanel.SetActive(false); hudPanel.SetActive(false); resultPanel.SetActive(true);
        resultText.text = summary;
    }

    public void SetStage(Stage s)      => stageText.text = $"{(int)s + 1}/8  {s}";
    public void SetScore(int score)    => scoreText.text = $"Score {score}";
    public void SetCount(int i, int n) => countText.text = $"{i}/{n}";
    public void Hint(string msg)       => hintText.text = msg;

    public void Flash(string msg, Color c)
    {
        feedbackText.text = msg;
        feedbackText.color = c;
        CancelInvoke(nameof(ClearFlash));
        Invoke(nameof(ClearFlash), 0.8f);
    }
    void ClearFlash() => feedbackText.text = "";
}
