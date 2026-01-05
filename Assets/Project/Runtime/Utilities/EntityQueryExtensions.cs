using Unity.Entities;

namespace SwarmSimulation.Runtime.Utilities
{
    public static class EntityQueryExtensions
    {
        public static bool TryGetSingletonEntity<T>(this SystemBase system, out Entity entity)
            where T : unmanaged, IComponentData
        {
            entity = system.EntityManager.CreateEntityQuery(typeof(T)).GetSingletonEntity();
            return entity != Entity.Null;
        }
    }
}