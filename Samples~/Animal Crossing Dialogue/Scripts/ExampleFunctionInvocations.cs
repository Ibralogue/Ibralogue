using System;
using Ibralogue;
using UnityEngine;
using UnityEngine.UI;

public static class ExampleFunctionInvocations
{
    [DialogueFunction]
    public static void TriggerFunction(DialogueManagerBase<Button> dialogueManager)
    {
        Debug.Log("Function Trigger");
        Debug.Log($"There are {dialogueManager.ParsedConversations.Count} conversations in this file.");
    }
        
    [DialogueFunction]
    public static string GetDay()
    {
        return DateTime.Now.DayOfWeek.ToString();
    }
}
