using UnityEngine;

public static class DialogueLogger
{
    public static void LogError(int line, string message, Object context = null)
    {
        Report(line,"", message, context);
    }

    private static void Report(int line, string where, string message, Object context)
    {
        if (line != -1)
        {
            Debug.LogError($"[Ibralogue] [line {line}], Error{where}: {message}", context);
        }
        else
        {
            Debug.LogError($"[Ibralogue] Error{where}: {message}", context);
        }
    }
}