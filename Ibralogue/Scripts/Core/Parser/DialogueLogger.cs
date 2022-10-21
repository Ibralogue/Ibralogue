using UnityEngine;

public static class DialogueLogger
{
    public static void LogError(int line, string message)
    {
        Report(line,"", message);
    }

    private static void Report(int line, string where, string message, Object context = null)
    {
        Debug.LogError($"[Ibralogue] [line {line}], Error{where}: {message}", context);
    }
}