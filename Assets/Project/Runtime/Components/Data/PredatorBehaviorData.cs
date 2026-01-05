using Unity.Entities;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct PredatorBehaviorData : IComponentData
    {
        public float AttackRange;
        public float AttackCooldown;
        public float LastAttackTime;
        public Entity CurrentTarget;
        public float TargetLockDistance;
        public float HungerLevel;
        public float MaximumHunger;
    }
}
