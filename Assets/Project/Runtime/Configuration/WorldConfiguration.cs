using Unity.Entities;

namespace SwarmSimulation.Runtime.Configuration
{
    public static class WorldConfiguration
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            DefaultWorldInitialization.DefaultLazyEditModeInitialize();
        }
    }
}