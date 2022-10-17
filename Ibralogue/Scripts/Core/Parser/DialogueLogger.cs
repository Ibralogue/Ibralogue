using UnityEngine;

public class DialogueLogger : MonoBehaviour
{
    public static void LogError(int line, string message)
    {
        Report(line,"", message);
    }

    private static void Report(int line, string where, string message)
    {
        Debug.LogError($"[Ibralogue] [line {line}], Error{where}: {message}");
    }
}