using Unity.Entities;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct AgentStateData : IComponentData
    {
        public float CurrentEnergy;
        public float MaximumEnergy;
        public float EnergyConsumptionRate;
        public float FearLevel;
        public float AggressionLevel;
        public float CuriosityLevel;
    }
}