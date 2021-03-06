using UnityEngine;

public static class VoxelData
{
    public static readonly int ChunkWidth = 16;
    public static readonly int ChunkHeight = 128;
    public static readonly int WordSizeInChunks = 15;
    public static readonly int ViewDistanceInChunks = 5;
     
    //Сколько в атласе блоков в одной строке
    public static readonly int TextureAtlasSizeInBlocks = 16;
    public static float NormalizedBlockTextureSize => 1f / (float) TextureAtlasSizeInBlocks;
    public static int WorldSizeInVoxels => WordSizeInChunks * ChunkWidth;
    
    // Массив точек(вершин) одного векселя(куба), а именно их координаты
    public static readonly Vector3[] voxelVerts = new Vector3[8]
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };
    
    // Массив проверок на необходимость отрисовки граней 
    public static readonly Vector3[] faceChecks = new Vector3[6]
    {
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
    };
    
    // Грани куба,отрисованные по точкам массива выше.
    public static readonly int[,] voxelTris = new int[6, 4]
    {
        // Back, Front, Top, Bottom, Left, Right
        {0,3,1,2}, // back
        {5,6,4,7}, // front
        {3,7,2,6}, // top
        {1,5,0,4}, // bottom
        {4,7,0,3}, // left
        {1,2,5,6}  // right 
    };

    // Порядок отрисовки текстуры по вершинам грани. Два треугольника.  
    public static readonly Vector2[] voxelUvs = new Vector2[4]
    {
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f)
    };
}
