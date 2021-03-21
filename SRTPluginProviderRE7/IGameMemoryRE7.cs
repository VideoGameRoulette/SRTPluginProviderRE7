﻿using System;
using System.Collections.Generic;

namespace SRTPluginProviderRE7
{
    public interface IGameMemoryRE7
    {
        float CurrentDA { get; set; }
        float MaxDA { get; set; }
        float CurrentHP { get; set; }
        float MaxHP { get; set; }
        long ItemCount { get; set; }
        List<string> ItemNames { get; set; }
        List<int> ItemQuantity { get; set; }
        List<byte> ItemSlot { get; set; }
    }
}
