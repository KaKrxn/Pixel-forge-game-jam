public static class SceneLoadRequest
{
    public static string TargetSceneName { get; private set; }

    public static void SetTarget(string sceneName)
    {
        TargetSceneName = sceneName;
    }

    public static string ConsumeTarget(string fallbackSceneName)
    {
        string target = string.IsNullOrWhiteSpace(TargetSceneName)
            ? fallbackSceneName
            : TargetSceneName;

        TargetSceneName = null;
        return target;
    }
}
