using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class World : MonoBehaviour
{
    // Сид вставить в offset шума, для рандомной генирации 
    public int seed;

    public Transform player;
    public Vector3 spawnPosition;
    public BiomeAttribute biome;

    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockTypes;
    public ChunkCoord playerChunkCoord;
    
    [FormerlySerializedAs("debugScreen")] public GameObject debugPanel;
    
    private Chunk[,] chunks = new Chunk[VoxelData.WordSizeInChunks, VoxelData.WordSizeInChunks];
    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    private List<Chunk> chunksToUpdate = new List<Chunk>();
    private List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    private Queue<VoxelMod> modifications = new Queue<VoxelMod>();
    private ChunkCoord playerLastChunkCoord;
    private bool applyingModifications = false;

    private void Start()
    {
        Random.InitState(seed);
        spawnPosition = new Vector3((VoxelData.WordSizeInChunks * VoxelData.ChunkWidth) / 2f,
            VoxelData.ChunkHeight + - 50f,
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

        if (modifications.Count > 0 && !applyingModifications)
        {
            StartCoroutine(nameof(ApplyModifications));
        }

        if (chunksToCreate.Count > 0)
        {
            CreateChunk();
        }

        if (chunksToUpdate.Count > 0)
        {
            UpDateChunks();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            debugPanel.SetActive(!debugPanel.activeSelf);
        }
    }
    
    // Создание мира по условиям
    public byte GetVoxel(Vector3 pos)
    {
        // Этот индекс БЕРЕТСЯ из объекта со скриптом WORLD, а НЕ СПРАЙТА 
        byte voxelValue = 3;
        int yPos = Mathf.FloorToInt(pos.y);

        if (!IsVoxelInWorldSizeFit(pos)) return 0; // Не в мире = блок "воздуха"
        if (yPos == 0) return 1; // Последний блок = бедрок
        
        int terrainHeight = // высота терейна 
            Mathf.FloorToInt(biome.terrainHeight *
                             Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) +
            biome.solidGroundHeight;
        
        // Поверхность чанка
        if (yPos == terrainHeight) voxelValue = 3; 
        // Чертыре блока вниз от повехности
        else if (yPos < terrainHeight && yPos > terrainHeight - 4) voxelValue = 2; 
        // спавн воздуха выше высоты террейна
        else if (yPos > terrainHeight) return 0; 
        // Спавн камня 
        else voxelValue = 1;

        // Условние спавна блоков биома по 3д шуму
        if (voxelValue == 2)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos <lode.maxHeight) 
                {
                    if (Noise.Get3DPerlin(pos,lode.noiseOffset,lode.scale,lode.threshold))
                    {
                        voxelValue = lode.blockID;
                    }
                }
            }
        }
        
        // Спавн деревьев. 
        if (yPos == terrainHeight)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x,pos.z),0,biome.treeZoneScale) > biome.treeZoneThreshold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x,pos.z),0,biome.treePlacementScale) > biome.treePlacementThreshold)
                {
                    Structures.MakeTree(pos,modifications,biome.mixTreeHeight,biome.maxTreeHeight);
                }
            }
        }
        return voxelValue;
    }

    public bool CheckIsVoxelSolid(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorldSizeFit(thisChunk)||pos.y< 0 || pos.y > VoxelData.ChunkHeight)
        {
            return false;
        }

        if (chunks[thisChunk.x,thisChunk.z] != null && chunks[thisChunk.x,thisChunk.z].isVoxelMapPopulated)
        {
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blockTypes[GetVoxel(pos)].isSolid;
    }
    
    public bool CheckIsVoxelTransparent(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorldSizeFit(thisChunk)||pos.y< 0 || pos.y > VoxelData.ChunkHeight)
        {
            return false;
        }

        if (chunks[thisChunk.x,thisChunk.z] != null && chunks[thisChunk.x,thisChunk.z].isVoxelMapPopulated)
        {
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;
        }

        return blockTypes[GetVoxel(pos)].isTransparent;
    }
    
    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        return chunks[x,z];
    }

    private void CreateChunk()
    {
        ChunkCoord cc = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        activeChunks.Add(cc);
        chunks[cc.x, cc.z].Init();
    }

    private void UpDateChunks()
    {
        bool updated = false;
        int index = 0;
        
        while (!updated && index < chunksToUpdate.Count - 1)
        {
            if (chunksToUpdate[index].isVoxelMapPopulated)
            {
                chunksToUpdate[index].UpdateChunk();
                chunksToUpdate.RemoveAt(index);
                updated = true;
            }
            else
            {
                index++;
            }
        }
    }

    private IEnumerator ApplyModifications()
    {
        int count = 0;
        /*
         *  \ Волшебное число, для регулирования скорость спавна деревьев в новом чанке
         * Если уменьшить - чанки генератся быстрее, а деревья медленне и наоборот
         * Изменять число В УСЛОВИИ ниже
         */
        
        
        applyingModifications = true;
        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            ChunkCoord c = GetChunkCoordFromVector3(v.position);

            if(chunks[c.x,c.z] == null)
            {
                chunks[c.x, c.z] = new Chunk(c, this, true);
                activeChunks.Add(c);
            }
            chunks[c.x, c.z].modifications.Enqueue(v);
            
            if (!chunksToUpdate.Contains(chunks[c.x,c.z]))
            {
                chunksToUpdate.Add(chunks[c.x,c.z]);
            }

            count++;
            if (count > 200) // ИЗМЕНЯТЬ ТУТ 
            {
                count = 0;
                yield return null;
            }
        }

        applyingModifications = false;
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
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, true);
                activeChunks.Add(new ChunkCoord(x,z));
            }
        }

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            ChunkCoord c = GetChunkCoordFromVector3(v.position);

            if(chunks[c.x,c.z] == null)
            {
                chunks[c.x, c.z] = new Chunk(c, this, true);
                activeChunks.Add(c);
            }
            chunks[c.x, c.z].modifications.Enqueue(v);
            
            if (!chunksToUpdate.Contains(chunks[c.x,c.z]))
            {
                chunksToUpdate.Add(chunks[c.x,c.z]);
            }
        }

        for (int i = 0; i < chunksToUpdate.Count; i++)
        {
            chunksToUpdate[0].UpdateChunk();
            chunksToUpdate.RemoveAt(0);
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
        playerLastChunkCoord = playerChunkCoord;
        
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = coord.x - VoxelData.ViewDistanceInChunks; x < coord.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++)
            {
                if (IsChunkInWorldSizeFit(new ChunkCoord(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                        chunksToCreate.Add(new ChunkCoord(x,z));
                    }
                    else if (!chunks[x, z].IsActive)
                    {
                        chunks[x, z].IsActive = true;
                        
                    }
                    activeChunks.Add(new ChunkCoord(x, z));
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
    public bool isTransparent;
    public Sprite icon;
    
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
public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0; 
    }
    public VoxelMod(Vector3 position, byte id)
    {
        this.id = id;
        this.position = position;
    }
}