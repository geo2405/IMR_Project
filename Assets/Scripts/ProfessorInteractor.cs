using UnityEngine;

public class ProfessorInteractor : MonoBehaviour
{
    private ProfessorProfile profile;
    [Header("Audio")]
    public AudioClip enterSfx;

    void Start()
    {
        profile = GetComponent<ProfessorProfile>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlaySfx(enterSfx);

        ProfessorChat chat =
            GetComponentInChildren<ProfessorChat>();

        ProfessorManager.Instance.SetActiveProfessor(
            GetComponent<ProfessorProfile>(),
            chat
        );
    }

    void PlaySfx(AudioClip clip)
    {
        var audio = AudioManager.EnsureExists();
        var chosen = clip ?? audio.defaultSfx;
        if (chosen == null) return;
        audio.PlayOneShot(chosen);
    }
}
