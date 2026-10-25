using UnityEngine;

public class NPCRoute : MonoBehaviour
{
    [SerializeField] string sceneName;
    [SerializeField] Transform[] points;

    public bool HasPoints => Points != null && Points.Length > 0;

    public Transform[] Points
    {
        get
        {
            if (points != null && points.Length > 0)
                return points;

            int count = transform.childCount;
            if (count == 0)
                return null;

            Transform[] children = new Transform[count];
            for (int i = 0; i < count; i++)
                children[i] = transform.GetChild(i);

            return children;
        }
    }

    public string SceneName => string.IsNullOrEmpty(sceneName)
        ? gameObject.scene.name
        : sceneName;

    public static NPCRoute FindForScene(string activeScene)
    {
        NPCRoute[] routes = FindObjectsByType<NPCRoute>(FindObjectsSortMode.None);
        for (int i = 0; i < routes.Length; i++)
        {
            if (routes[i] != null && routes[i].SceneName == activeScene)
                return routes[i];
        }

        return null;
    }
}
