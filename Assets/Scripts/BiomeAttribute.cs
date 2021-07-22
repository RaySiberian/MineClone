using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "BiomeAttributes")]
public class BiomeAttribute : ScriptableObject
{
    public string biomeName;

    public int solidGroundHeight;
    public int terrainHeight;
    public float terrainScale;

    public Lode[] lodes;
}

// Класс для настройки диапозона спавна блока 
[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}