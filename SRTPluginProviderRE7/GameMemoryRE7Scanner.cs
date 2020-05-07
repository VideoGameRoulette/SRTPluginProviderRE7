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
        private long pointerAddressHP;

        // Pointer Classes
        private long BaseAddress { get; set; }
        private MultilevelPointer PointerPlayerHP { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proc"></param>
        internal GameMemoryRE7Scanner(int? pid = null)
        {
            gameMemoryValues = new GameMemoryRE7();
            SelectPointerAddresses();
            if (pid != null)
            {
                Initialize(pid.Value);
            }

            // Setup the pointers.
            
        }

        internal void Initialize(int pid)
        {
            memoryAccess = new ProcessMemory.ProcessMemory(pid);

            if (ProcessRunning)
            {
                BaseAddress = NativeWrappers.GetProcessBaseAddress(pid, PInvoke.ListModules.LIST_MODULES_64BIT).ToInt64(); // Bypass .NET's managed solution for getting this and attempt to get this info ourselves via PInvoke since some users are getting 299 PARTIAL COPY when they seemingly shouldn't.

                PointerPlayerHP = new MultilevelPointer(memoryAccess, BaseAddress + pointerAddressHP, 0xC0L, 0x48L, 0x98L, 0x78L);
            }
        }

        private void SelectPointerAddresses()
        {
            pointerAddressHP = 0x081DF348;
        }


        /// <summary>
        /// 
        /// </summary>
        internal void UpdatePointers()
        {
            PointerPlayerHP.UpdatePointers();
        }

        internal IGameMemoryRE7 Refresh()
        {
            // Player HP
            gameMemoryValues.PlayerCurrentHealth = PointerPlayerHP.DerefInt(0x20);
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
