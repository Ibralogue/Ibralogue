using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ibralogue.Examples
{
    public static class ExampleFunctionInvocations
    {
        [DialogueFunction]
        public static void TriggerFunction(DialogueManagerBase<Button> dialogueManager)
        {
            Debug.Log($"There are {dialogueManager.ParsedConversations.Count} conversations in this file.");
            Debug.Log("Function Trigger");
        }

        [DialogueFunction]
        public static string GetDay()
        {
            return DateTime.Now.DayOfWeek.ToString();
        }
    }
}
