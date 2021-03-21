using System;
using System.Globalization;
using System.Collections.Generic;

namespace SRTPluginProviderRE7
{
    public struct GameMemoryRE7 : IGameMemoryRE7
    {
        public float CurrentDA { get; set; }
        public float MaxDA { get; set; }
        public float CurrentHP { get; set; }
        public float MaxHP { get; set; }
        public long ItemCount { get; set; }
        public List<string> ItemNames { get; set; }
        public List<int> ItemQuantity { get; set; }
        public List<byte> ItemSlot { get; set; }

    }
}
