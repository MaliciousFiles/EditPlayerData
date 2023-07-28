using System.Collections.Generic;

namespace EditPlayerData.Utils;

public class TowerTiersEqualityComparer : IEqualityComparer<int[]>
{
    public bool Equals(int[]? x, int[]? y)
    {
        // only fires if GetHashCode is the same, and I can guarantee no collisions
        return true;
    }

    public int GetHashCode(int[] obj)
    {
        return 100 * obj[0] + 10 * obj[1] + obj[2];
    }
}