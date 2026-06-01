using UnityEngine;

public static class NoiseEmitter
{
    public enum NoiseType { Footstep, Attack, LandImpact }

    public static event System.Action<Vector3, float, NoiseType, GameObject> OnNoiseEmitted;

    public static void EmitNoise(Vector3 worldPos, float radius, NoiseType type, GameObject source)
        => OnNoiseEmitted?.Invoke(worldPos, radius, type, source);
}
