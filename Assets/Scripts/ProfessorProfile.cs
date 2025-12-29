using UnityEngine;

public class ProfessorProfile : MonoBehaviour
{
    public string professorName;
    public string domain;

    [TextArea(2, 5)]
    public string systemPrompt;

    public string[] keywords;
}
