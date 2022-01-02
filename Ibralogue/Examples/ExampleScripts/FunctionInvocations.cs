using System;
using Ibralogue;
using UnityEngine;

public static class FunctionInvocations
{
    [DialogueFunction]
    public static void TriggerFunction()
    {
        Debug.Log("Function Trigger");
    }
        
    [DialogueFunction]
    public static string GetDay()
    {
        return DateTime.Now.DayOfWeek.ToString();
    }
}
