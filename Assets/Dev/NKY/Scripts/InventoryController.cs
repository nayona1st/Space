using Dev.NKY.Scripts;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryGrid grid;
    [SerializeField] private PlayerStats stats;
 
    private void OnEnable()
    {
        grid.OnBlockPlaced += stats.ApplyModifiers;
        grid.OnBlockRemoved += stats.RemoveModifiers;
    }
 
    private void OnDisable()
    {
        grid.OnBlockPlaced -= stats.ApplyModifiers;
        grid.OnBlockRemoved -= stats.RemoveModifiers;
    }
}
