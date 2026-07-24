using System.Collections.Generic;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    [CreateAssetMenu(fileName = "BlockData", menuName = "So/BlockData", order = 0)]
    public class BlockData : ScriptableObject
    {
        public string blockName;
        public Sprite icon;
 
        // 인스펙터에서 이렇게 그리면 됨 (X = 칸 있음, . 또는 공백 = 칸 없음)
        // 예: L자 블록
        // X.
        // X.
        // XX
        [TextArea(1, 6)]
        public string shapePattern = "X";
 
        // shapePattern을 파싱한 결과. 코드에서는 이걸 사용 (수동 입력 X)
        [HideInInspector] public List<Vector2Int> cells = new List<Vector2Int>();
 
        private void OnValidate()
        {
            cells = ParseShape(shapePattern);
        }
 
        private static List<Vector2Int> ParseShape(string pattern)
        {
            var result = new List<Vector2Int>();
            if (string.IsNullOrEmpty(pattern)) return result;
 
            var lines = pattern.Replace("\r", "").Split('\n');
            for (int row = 0; row < lines.Length; row++)
            {
                var line = lines[row];
                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] == 'X')
                        result.Add(new Vector2Int(col, row)); // row가 곧 grid의 y (위→아래로 증가)
                }
            }
            return result;
        }
    }
}