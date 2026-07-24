using System.Collections.Generic;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    [CreateAssetMenu(fileName = "BlockList", menuName = "So/BlockList", order = 0)]
    public class BlockDataList : ScriptableObject
    {
        public List<BlockData> blocks;
    }
}