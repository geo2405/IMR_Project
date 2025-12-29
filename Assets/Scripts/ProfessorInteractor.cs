using UnityEngine;

public class ProfessorInteractor : MonoBehaviour
{
    private ProfessorProfile profile;

    void Start()
    {
        profile = GetComponent<ProfessorProfile>();
    }

    void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player")) return;

    ProfessorChat chat =
        GetComponentInChildren<ProfessorChat>();

    ProfessorManager.Instance.SetActiveProfessor(
        GetComponent<ProfessorProfile>(),
        chat
    );
}
}
