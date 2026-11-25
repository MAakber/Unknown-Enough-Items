// Controllable camera/scene boundaries; these do not execute Unity rendering.
namespace UnityEngine
{
    public sealed class Canvas : MonoBehaviour { }
    public sealed class Camera : MonoBehaviour
    {
        public static Camera? main = new();
        public Func<Vector3, Vector3> Project = position => position;
        public Vector3 ScreenToWorldPoint(Vector3 position) => Project(position);
    }
    public static class Screen { public static int width = 1280; public static int height = 720; }
}
namespace UnityEngine.SceneManagement
{
    public readonly struct Scene(int sceneHandle) { public int handle => sceneHandle; }
    public static class SceneManager
    {
        public static int ActiveHandle = 1;
        public static Scene GetActiveScene() => new(ActiveHandle);
    }
}
namespace UnityEngine.EventSystems
{
    public struct RaycastResult { public object? module; }
}
namespace UnityEngine.UI
{
    public class GraphicRaycaster { }
}
