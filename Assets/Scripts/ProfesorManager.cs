using UnityEngine;

public class ProfessorManager : MonoBehaviour
{
    public static ProfessorManager Instance;

    public ProfessorProfile activeProfessor;
    public ProfessorChat activeChat;

    void Awake()
    {
        Instance = this;
    }

    public void SetActiveProfessor(
        ProfessorProfile prof,
        ProfessorChat chat)
    {
        activeProfessor = prof;
        activeChat = chat;

        Debug.Log($"Profesor activ: {prof.professorName}");
        Debug.Log($"Chat activ: {chat.gameObject.name}");
    }
}
