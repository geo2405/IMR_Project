using System.Collections.Generic;

public class ConversationLog
{
    public List<string> studentMessages = new();
    public List<string> professorMessages = new();

    public string FullText()
    {
        return string.Join(" ", studentMessages) + " " +
               string.Join(" ", professorMessages);
    }

    public void Clear()
    {
        studentMessages.Clear();
        professorMessages.Clear();
    }
}
