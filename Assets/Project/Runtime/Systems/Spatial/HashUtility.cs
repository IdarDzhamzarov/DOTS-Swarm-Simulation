using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Systems.Spatial
{
    public static class HashUtility
    {
        public static int CalculateSpatialHash(int3 gridCell)
        {
            const int prime1 = 73856093;
            const int prime2 = 19349663;
            const int prime3 = 83492791;
            return gridCell.x * prime1 ^ gridCell.y * prime2 ^ gridCell.z * prime3;
        }

        public static int3 DehashSpatialHash(int hash)
        {
            return new int3(
                (hash >> 16) & 0xFF,
                (hash >> 8) & 0xFF,
                hash & 0xFF
            );
        }
    }
}