using UnityEngine;

public class Noise
{
    public static float Get2DPerlin(Vector2 pos, float offset, float scale)
    {
        return Mathf.PerlinNoise((pos.x + 0.1f) / VoxelData.ChunkWidth * scale + offset,
            (pos.y + 0.1f) / VoxelData.ChunkWidth * scale + offset);
    }
}