using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Utilities
{
    public static class MathematicsExtensions
    {
        public static float3 SafeNormalize(this float3 vector, float epsilon = 1e-6f)
        {
            float magnitude = math.length(vector);
            return magnitude > epsilon ? vector / magnitude : float3.zero;
        }

        public static float SmoothStep(this float value, float edge0, float edge1)
        {
            float t = math.clamp((value - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        public static float3 RotateTowards(this float3 current, float3 target, float maxRadiansDelta)
        {
            float currentMagnitude = math.length(current);
            float targetMagnitude = math.length(target);

            if (currentMagnitude < 1e-6f || targetMagnitude < 1e-6f)
                return target;

            float3 currentNormalized = current / currentMagnitude;
            float3 targetNormalized = target / targetMagnitude;

            float dotProduct = math.dot(currentNormalized, targetNormalized);
            dotProduct = math.clamp(dotProduct, -1f, 1f);

            float angle = math.acos(dotProduct);
            float t = math.min(1f, maxRadiansDelta / angle);

            return math.lerp(currentNormalized, targetNormalized, t) *
                   math.lerp(currentMagnitude, targetMagnitude, t);
        }
    }
}