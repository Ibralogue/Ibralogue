using System;
using UnityEngine;

namespace Ibralogue.Examples
{
    public static class ExampleInvocations
    {
        [DialogueInvocation]
        public static void TriggerFunction(DialogueEngineBase engine)
        {
            Debug.Log($"There are {engine.ParsedConversations.Count} conversations in this file.");
        }

        [DialogueInvocation]
        public static string GetDay()
        {
            return DateTime.Now.DayOfWeek.ToString();
        }
    }
}
