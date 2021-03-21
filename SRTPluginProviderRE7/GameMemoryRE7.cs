using System;
using System.Globalization;
using System.Collections.Generic;

namespace SRTPluginProviderRE7
{
    public class GameMemoryRE7 : IGameMemoryRE7
    {
        public float CurrentDA { get; set; }
        public float MaxDA { get; set; }
        public float CurrentHP { get; set; }
        public float MaxHP { get; set; }
        public long ItemCount { get; set; }
        public List<string> ItemNames { get; set; }
        public List<int> ItemQuantity { get; set; }
        public List<byte> ItemSlot { get; set; }

        public GameMemoryRE7()
        {
            CurrentDA = 0;
            MaxDA = 0;
            CurrentHP = 0;
            MaxHP = 0;
            ItemCount = 0;
            ItemNames = new List<string>();
            ItemQuantity = new List<int>();
            ItemSlot = new List<byte>();
        }
    }
}
