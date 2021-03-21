using ProcessMemory;
using System;
using System.ComponentModel.Design;

namespace SRTPluginProviderRE7
{
    internal class GameMemoryRE7Scanner : IDisposable
    {
        // Variables
        private ProcessMemory.ProcessMemory memoryAccess;
        private GameMemoryRE7 gameMemoryValues;
        public bool HasScanned;
        public bool ProcessRunning => memoryAccess != null && memoryAccess.ProcessRunning;
        public int ProcessExitCode => (memoryAccess != null) ? memoryAccess.ProcessExitCode : 0;

        // Pointer Address Variables
        private long difficultyAdjustment;
        private long hitPoints;
        private long itemCount;

        // Pointer Classes
        private long BaseAddress { get; set; }
        private MultilevelPointer PointerDA { get; set; }
        private MultilevelPointer PointerHP { get; set; }
        private MultilevelPointer PointerItemCount { get; set; }

        private MultilevelPointer[] PointerItemLength { get; set; }
        private MultilevelPointer[] PointerItemNames { get; set; }
        private MultilevelPointer[] PointerItemQuantity { get; set; }
        private MultilevelPointer[] PointerItemSlot { get; set; }

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
            SelectPointerAddressesSteam();
            //SelectPointerAddressesWindows();
            memoryAccess = new ProcessMemory.ProcessMemory(pid);

            if (ProcessRunning)
            {
                BaseAddress = NativeWrappers.GetProcessBaseAddress(pid, PInvoke.ListModules.LIST_MODULES_64BIT).ToInt64(); // Bypass .NET's managed solution for getting this and attempt to get this info ourselves via PInvoke since some users are getting 299 PARTIAL COPY when they seemingly shouldn't.

                PointerDA = new MultilevelPointer(memoryAccess, BaseAddress + difficultyAdjustment);
                PointerHP = new MultilevelPointer(memoryAccess, BaseAddress + hitPoints, 0xA0L, 0xD0L, 0x70L);
                PointerItemCount = new MultilevelPointer(memoryAccess, BaseAddress + itemCount, 0x60L);
                GetItems();
            }
        }

        private void GetItems()
        {
            if (gameMemoryValues.ItemCount != 0)
            {
                for (var i = 0; i < gameMemoryValues.ItemCount; i++)
                {
                    long position = (0x30L + (0x8L * i));
                    PointerItemLength[i] = new MultilevelPointer(memoryAccess, BaseAddress + itemCount, 0x60L, 0x20L, position, 0x28L, 0x80L, 0x20L);
                    PointerItemNames[i] = new MultilevelPointer(memoryAccess, BaseAddress + itemCount, 0x60L, 0x20L, position, 0x28L, 0x80L, 0x24L);
                    PointerItemQuantity[i] = new MultilevelPointer(memoryAccess, BaseAddress + itemCount, 0x60L, 0x20L, position, 0x28L, 0x88L);
                    PointerItemSlot[i] = new MultilevelPointer(memoryAccess, BaseAddress + itemCount, 0x60L, 0x20L, position, 0x28L, 0xB0L);
                }
            }
        }

        private void UpdateItems()
        {
            if (gameMemoryValues.ItemCount != 0)
            {
                for (var i = 0; i < gameMemoryValues.ItemCount; i++)
                {
                    PointerItemLength[i].UpdatePointers();
                    PointerItemNames[i].UpdatePointers();
                    PointerItemQuantity[i].UpdatePointers();
                    PointerItemSlot[i].UpdatePointers();
                }
            }
        }

        private void RefreshItems()
        {
            if (gameMemoryValues.ItemCount != 0)
            {
                for (var i = 0; i < gameMemoryValues.ItemCount; i++)
                {
                    var length = PointerItemLength[i].DerefInt(0x20);
                    var bytes = PointerItemNames[i].DerefByteArray(0x24, length * 2);
                    gameMemoryValues.ItemNames[i] = System.Text.Encoding.Unicode.GetString(bytes);
                    gameMemoryValues.ItemQuantity[i] = PointerItemQuantity[i].DerefInt(0x88);
                    gameMemoryValues.ItemSlot[i] = PointerItemSlot[i].DerefByte(0xB0);
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
            hitPoints = 0x09373DB8;
            itemCount = 0x093352C0;
        }


        /// <summary>
        /// 
        /// </summary>
        internal void UpdatePointers()
        {
            PointerDA.UpdatePointers();
            PointerHP.UpdatePointers();
            PointerItemCount.UpdatePointers();
            UpdateItems();
        }

        internal IGameMemoryRE7 Refresh()
        {
            gameMemoryValues.CurrentDA = PointerDA.DerefFloat(0xF8);
            gameMemoryValues.MaxDA = PointerDA.DerefFloat(0xFC);
            gameMemoryValues.CurrentHP = PointerHP.DerefFloat(0x24);
            gameMemoryValues.MaxHP = PointerHP.DerefFloat(0x20);
            gameMemoryValues.ItemCount = PointerItemCount.DerefLong(0x28);
            RefreshItems();

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
