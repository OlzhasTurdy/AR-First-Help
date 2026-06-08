using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public static class ARSceneLoadGuard
{
    private const string ARSceneName = "ARScene";
    private static bool isLoadingARScene;

    static ARSceneLoadGuard()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void LoadARScene()
    {
        if (isLoadingARScene)
        {
            Debug.LogWarning("[AR] ARScene is already loading. Ignoring duplicate request.");
            return;
        }

        isLoadingARScene = true;
        StopActiveARSessions();
        SceneManager.LoadScene(ARSceneName);
    }

    private static void StopActiveARSessions()
    {
        ARSession[] sessions = Object.FindObjectsOfType<ARSession>(true);
        foreach (ARSession session in sessions)
        {
            if (session != null)
                session.enabled = false;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == ARSceneName)
            isLoadingARScene = false;
    }
}
