using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreDisplay : MonoBehaviour
{
    public static ScoreDisplay Instance;

    [Header("UI Elemente")]
    public GameObject panelScor;
    public TMP_Text textTitlu;
    public TMP_Text textProcent;
    public TMP_Text textFeedback;

    void Awake()
    {
        if (Instance == null) Instance = this;
        if (panelScor != null) panelScor.SetActive(false);
    }

    public void ArataScorComplet(int scor, string explicatie)
    {
        if (panelScor == null) return;

        panelScor.SetActive(true);

        if (textProcent != null)
        {
            textProcent.text = scor + "%";
            if (scor >= 50) textProcent.color = Color.green;
            else textProcent.color = Color.red;
        }

        if (textFeedback != null)
        {
            textFeedback.text = explicatie;
        }

        StopAllCoroutines();
        StartCoroutine(AscundeDupaTimp());
    }

    IEnumerator AscundeDupaTimp()
    {
        yield return new WaitForSeconds(10.0f);
        panelScor.SetActive(false);
    }
}