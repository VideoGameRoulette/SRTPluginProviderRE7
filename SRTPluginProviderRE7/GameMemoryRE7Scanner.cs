using ProcessMemory;
using System;
using System.ComponentModel.Design;
using SRTPluginProviderRE7.Structs;

namespace SRTPluginProviderRE7
{
    internal class GameMemoryRE7Scanner : IDisposable
    {
        // Variables
        private ProcessMemoryHandler memoryAccess;
        private GameMemoryRE7 gameMemoryValues;
        public bool HasScanned;
        public bool ProcessRunning => memoryAccess != null && memoryAccess.ProcessRunning;
        public int ProcessExitCode => (memoryAccess != null) ? memoryAccess.ProcessExitCode : 0;
        private int EnemyTableCount;

        // Pointer Address Variables
        private long difficultyAdjustment;
        private long hitPoints;
        private long itemCount;
        private long mapName;
        private long bagCount;

        // Pointer Classes
        private long BaseAddress { get; set; }
        private MultilevelPointer PointerDA { get; set; }
        private MultilevelPointer PointerMapName { get; set; }
        private MultilevelPointer PointerHP { get; set; }
        private MultilevelPointer PointerBagCount { get; set; }
        private MultilevelPointer PointerInventoryCount { get; set; }
        private MultilevelPointer PointerEnemyEntryCount { get; set; }
        private MultilevelPointer[] PointerEnemyEntries { get; set; }
        private MultilevelPointer[] PointerItemNames { get; set; }
        private MultilevelPointer[] PointerItemInfo { get; set; }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proc"></param>
        internal GameMemoryRE7Scanner(int? pid = null)
        {
            gameMemoryValues = new GameMemoryRE7();

            if (pid != null)
            {
                Initialize(pid.Value);
            }

            // Setup the pointers.
            
        }

        internal void Initialize(int pid)
        {
            //SelectPointerAddressesSteam();
            SelectPointerAddressesWindows();
            memoryAccess = new ProcessMemoryHandler(pid);

            if (ProcessRunning)
            {
                BaseAddress = NativeWrappers.GetProcessBaseAddress(pid, PInvoke.ListModules.LIST_MODULES_64BIT).ToInt64(); // Bypass .NET's managed solution for getting this and attempt to get this info ourselves via PInvoke since some users are getting 299 PARTIAL COPY when they seemingly shouldn't.

                PointerDA = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + difficultyAdjustment));
                PointerMapName = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + mapName), 0x700L);
                PointerHP = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + hitPoints), 0x58L, 0xB0L, 0x70L, 0x20L, 0x0, 0x70L);
                //PointerHP = new MultilevelPointer(memoryAccess, BaseAddress + hitPoints, 0xA0L, 0xD0L, 0x70L);
                PointerEnemyEntryCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + hitPoints), 0x190L, 0x70);
                GenerateEnemyEntries();

                PointerBagCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + bagCount));
                PointerInventoryCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + itemCount), 0x60L);
                PointerItemNames = new MultilevelPointer[32];
                PointerItemInfo = new MultilevelPointer[32];
            }
        }

        private void GenerateEnemyEntries()
        {
            EnemyTableCount = PointerEnemyEntryCount.DerefInt(0x820) - 1; // Get the size of the enemy pointer table. This seems to double (4, 8, 16, 32, ...) but never decreases, even after a new game is started.
            if (PointerEnemyEntries == null || PointerEnemyEntries.Length != EnemyTableCount) // Enter if the pointer table is null (first run) or the size does not match.
            {
                long position = 0;
                PointerEnemyEntries = new MultilevelPointer[EnemyTableCount]; // Create a new enemy pointer table array with the detected size.
                
                // Loop through and create all of the pointers for the table.
                for (long i = 0; i < PointerEnemyEntries.Length; ++i)
                {
                    position = 0x8L + (i * 0x08L);
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + hitPoints), 0x58L, 0xB0L, 0x70L, 0x20L, position, 0x70L);
                }
            }
        }

        private void GetItems()
        {
            if (gameMemoryValues.PlayerInventoryCount != 0)
            {
                for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
                {
                    long position = (0x30L + (0x8L * i));
                    PointerItemNames[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + itemCount), 0x60L, 0x20L, position, 0x28L, 0x80L);
                    PointerItemInfo[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + itemCount), 0x60L, 0x20L, position, 0x28L);
                }
            }
            UpdateItems();
        }

        private void UpdateItems()
        {
            if (gameMemoryValues.PlayerInventoryCount != 0)
            {
                for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
                {
                    PointerItemNames[i].UpdatePointers();
                    PointerItemInfo[i].UpdatePointers();
                }
            }
            RefreshItems();
        }

        private void RefreshItems()
        {
            if (gameMemoryValues.PlayerInventoryCount != 0 && gameMemoryValues.PlayerInventory == null)
            {
                gameMemoryValues.PlayerInventory = new InventoryEntry[20];
                for (int i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
                {
                    gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
                }
                for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
                {
                    var length = PointerItemNames[i].DerefInt(0x20);
                    if (length > 0)
                    {
                        var bytes = PointerItemNames[i].DerefByteArray(0x24, length * 2);
                        gameMemoryValues.PlayerInventory[i].SetValues(PointerItemInfo[i].DerefByte(0xB0), System.Text.Encoding.Unicode.GetString(bytes), PointerItemInfo[i].DerefInt(0x88));
                    }
                }
                if (gameMemoryValues.PlayerInventoryCount < 20)
                {
                    for (var j = gameMemoryValues.PlayerInventoryCount; j < 20; j++)
                    {
                        gameMemoryValues.PlayerInventory[j].SetValues(-1, null, -1);
                    }
                }
            }
        }

        private void SelectPointerAddressesSteam()
        {
            difficultyAdjustment = 0x081FA818;
            hitPoints = 0x081EA150;
            itemCount = 0x081F1308;
        }

        private void SelectPointerAddressesWindows()
        {
            difficultyAdjustment = 0x0933E618;
            itemCount = 0x093352C0;
            hitPoints = 0x09417178;
            mapName = 0x0932F7E8;
            bagCount = 0x9373DB8;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void UpdatePointers()
        {
            PointerDA.UpdatePointers();
            PointerMapName.UpdatePointers();
            PointerHP.UpdatePointers();
            PointerBagCount.UpdatePointers();
            PointerInventoryCount.UpdatePointers();

            PointerEnemyEntryCount.UpdatePointers();
            GenerateEnemyEntries(); // This has to be here for the next part.
            for (int i = 0; i < PointerEnemyEntries.Length; ++i)
                PointerEnemyEntries[i].UpdatePointers();
        }

        internal IGameMemoryRE7 Refresh()
        {
            var bytes = PointerMapName.DerefByteArray(0x0, 48);
            gameMemoryValues.MapName = System.Text.Encoding.Unicode.GetString(bytes);
            gameMemoryValues.CurrentDA = PointerDA.DerefFloat(0xF8);
            gameMemoryValues.CurrentHP = PointerHP.DerefFloat(0x24);
            gameMemoryValues.MaxHP = PointerHP.DerefFloat(0x20);
            gameMemoryValues.PlayerInventorySlots = (PointerBagCount.DerefInt(0x78) * 4) + 12;
            gameMemoryValues.PlayerInventoryCount = PointerInventoryCount.DerefInt(0x28);

            // Enemy HP
            GenerateEnemyEntries();
            if (gameMemoryValues.EnemyHealth == null || gameMemoryValues.EnemyHealth.Length < EnemyTableCount)
            {
                gameMemoryValues.EnemyHealth = new EnemyHP[EnemyTableCount];
                gameMemoryValues.EnemyCount = gameMemoryValues.EnemyHealth.Length;
                for (int i = 0; i < gameMemoryValues.EnemyHealth.Length; ++i)
                    gameMemoryValues.EnemyHealth[i] = new EnemyHP();
            }
            for (int i = 0; i < gameMemoryValues.EnemyHealth.Length; ++i)
            {
                if (i < PointerEnemyEntries.Length)
                { // While we're within the size of the enemy table, set the values.
                    gameMemoryValues.EnemyHealth[i].MaximumHP = PointerEnemyEntries[i].DerefFloat(0x20);
                    gameMemoryValues.EnemyHealth[i].CurrentHP = PointerEnemyEntries[i].DerefFloat(0x24);
                }
                else
                { // We're beyond the current size of the enemy table. It must have shrunk because it was larger before but for the sake of performance, we're not going to constantly recreate the array any time the size doesn't match. Just blank out the remaining array values.
                    gameMemoryValues.EnemyHealth[i].MaximumHP = 0;
                    gameMemoryValues.EnemyHealth[i].CurrentHP = 0;
                }
            }

            GetItems();
            HasScanned = true;
            return gameMemoryValues;
        }

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (memoryAccess != null)
                        memoryAccess.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~REmake1Memory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
#endregion
    }
}
