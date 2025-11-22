using UnityEngine;
using TMPro;

public class LLMUIController : MonoBehaviour
{
    public TMP_InputField promptInputField;
    public TextMeshProUGUI responseText;

    public void OnSendPrompt()
    {
        if (promptInputField == null || responseText == null)
            return;

        responseText.text = "You wrote: " + promptInputField.text;
    }
}
