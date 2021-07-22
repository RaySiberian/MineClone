using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public int seed;

    public Transform player;
    public Vector3 spawnPosition;
    public BiomeAttribute biome;

    public Material material;
    public BlockType[] blockTypes;

    private Chunk[,] chunks = new Chunk[VoxelData.WordSizeInChunks, VoxelData.WordSizeInChunks];
    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();

    private ChunkCoord playerChunkCoord;
    private ChunkCoord playerLastChunkCoord;
    
    // Создание мира по условиям Y
    public byte GetVoxel(Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);

        if (!IsVoxelInWorldSizeFit(pos)) return 0;
        if (yPos == 0) return 1;

        int terrainHeight =
            Mathf.FloorToInt(biome.terrainHeight *
                             Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) +
            biome.solidGroundHeight;

        if (yPos == terrainHeight) return 3; // Поверхность чанка
        else if (yPos < terrainHeight && yPos > terrainHeight - 4) return 6; // Чертыре блока вниз от повехности
        else if (yPos > terrainHeight) return 0; //
        else return 2;
    }

    private void Start()
    {
        Random.InitState(seed);
        spawnPosition = new Vector3((VoxelData.WordSizeInChunks * VoxelData.ChunkWidth) / 2f,
            VoxelData.ChunkHeight + 2f,
            (VoxelData.WordSizeInChunks * VoxelData.ChunkWidth) * 0.5f); // Resharper Душит тварына
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.transform.position);
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }
    }

    private void GenerateWorld()
    {
        for (int x = (VoxelData.WordSizeInChunks / 2) - VoxelData.ViewDistanceInChunks;
            x < (VoxelData.WordSizeInChunks / 2) + VoxelData.ViewDistanceInChunks;
            x++)
        {
            for (int z = (VoxelData.WordSizeInChunks / 2) - VoxelData.ViewDistanceInChunks;
                z < (VoxelData.WordSizeInChunks / 2) + VoxelData.ViewDistanceInChunks;
                z++)
            {
                CreateNewChunk(x, z);
            }
        }

        player.position = spawnPosition;
    }

    private ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        return new ChunkCoord(x, z);
    }

    private void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = coord.x - VoxelData.ViewDistanceInChunks; x < coord.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++)
            {
                if (IsChunkInWorldSizeFit(new ChunkCoord(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        CreateNewChunk(x, z);
                    }
                    else if (!chunks[x, z].IsActive)
                    {
                        chunks[x, z].IsActive = true;
                        activeChunks.Add(new ChunkCoord(x, z));
                    }
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (ChunkCoord c in previouslyActiveChunks)
        {
            chunks[c.x, c.z].IsActive = false;
        }
    }

    private void CreateNewChunk(int x, int z)
    {
        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this);
        activeChunks.Add(new ChunkCoord(x, z));
    }

    private bool IsChunkInWorldSizeFit(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < VoxelData.WordSizeInChunks - 1 &&
            coord.z > 0 && coord.z < VoxelData.WordSizeInChunks - 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool IsVoxelInWorldSizeFit(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels &&
            pos.y >= 0 && pos.y < VoxelData.ChunkHeight &&
            pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;

    [Header("Texture Values")] public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Порядок наложения текстур 
    // Back, Front, Top, Bottom, Left, Right

    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index.  Class World");
                return 0;
        }
    }
}