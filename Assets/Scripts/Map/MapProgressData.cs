using System.Collections.Generic;

public class MapProgressData
{
    public int currentLayer = -1;
    public int currentNodeIndex = -1;

    public HashSet<string> unlockedNodes = new();
    public HashSet<string> completedNodes = new();

    public string GetKey(int layer, int nodeIndex)
    {
        return $"{layer}_{nodeIndex}";
    }
}