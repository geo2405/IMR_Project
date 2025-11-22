using UnityEngine;
using Unity.XR.CoreUtils;
using TMPro;

public class TargetScore : MonoBehaviour
{
    [Header("References")]

    private Transform xrOrigin;
    private TextMeshProUGUI scoreText;
    
    public float maxScore = 100f;
    public float minScore = 5f;
    public float distanceMultiplier = 10f;

    void Start()
    {
        var origin = FindObjectOfType<XROrigin>();
        if (origin != null)
            xrOrigin = origin.transform;
        else
            Debug.LogWarning("XR Origin not found");

        var tmp = FindObjectOfType<TextMeshProUGUI>();
        if (tmp != null)
            scoreText = tmp;
        else
            Debug.LogWarning("TextMeshProUGUI (score text) not found!");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Throwable"))
        {
            var throwableScript = collision.gameObject.GetComponent<Throwable>();

            Vector3 releasePos;
            if (throwableScript != null)
                releasePos = throwableScript.lastReleasePosition;
            else
                releasePos = collision.transform.position;

            Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : collision.transform.position;

            float distance = Vector3.Distance(releasePos, contactPoint);

            float rawScore = minScore + distance * distanceMultiplier;
            float finalScore = Mathf.Clamp(rawScore, minScore, maxScore);

            if (scoreText != null)
                scoreText.text = "Scor: " + Mathf.RoundToInt(finalScore).ToString();

            StartCoroutine(FlashTarget(finalScore / maxScore));
        }
    }

    private System.Collections.IEnumerator FlashTarget(float scoreRatio)
    {
        var rend = GetComponent<Renderer>();
        if (rend == null) yield break;

        Color original = rend.material.color;
        Color targetColor = Color.Lerp(Color.red, Color.green, scoreRatio);

        float t = 0f;
        float dur = 0.4f;
        while (t < dur)
        {
            rend.material.color = Color.Lerp(original, targetColor, t / dur);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (t < dur)
        {
            rend.material.color = Color.Lerp(targetColor, original, t / dur);
            t += Time.deltaTime;
            yield return null;
        }

        rend.material.color = original;
    }
}
