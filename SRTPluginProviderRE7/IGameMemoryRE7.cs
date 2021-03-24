using System;
using System.Collections.Generic;
using SRTPluginProviderRE7.Structs;

namespace SRTPluginProviderRE7
{
    public interface IGameMemoryRE7
    {
        string MapName { get; set; }
        float CurrentDA { get; set; }
        float CurrentHP { get; set; }
        float MaxHP { get; set; }
        int EnemyCount { get; set; }
        EnemyHP[] EnemyHealth { get; set; }
        int PlayerInventoryCount { get; set; }
        int PlayerInventorySlots { get; set; }
        InventoryEntry[] PlayerInventory { get; set; }
    }
}
