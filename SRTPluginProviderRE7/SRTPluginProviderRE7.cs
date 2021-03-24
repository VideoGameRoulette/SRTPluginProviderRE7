using SRTPluginBase;
using System;
using System.Diagnostics;
using System.Linq;

namespace SRTPluginProviderRE7
{
    public class SRTPluginProviderRE7 : IPluginProvider
    {
        private int? processId;
        private GameMemoryRE7Scanner gameMemoryScanner;
        private Stopwatch stopwatch;
        private IPluginHostDelegates hostDelegates;
        public IPluginInfo Info => new PluginInfo();

        public bool get_GameRunning
        {
            get
            {
                if (gameMemoryScanner != null && !gameMemoryScanner.ProcessRunning)
                {
                    processId = GetProcessId();
                    if (processId != null)
                    {
                        gameMemoryScanner.Initialize((int)processId); // Re-initialize and attempt to continue.
                    }
                }

                return gameMemoryScanner != null && gameMemoryScanner.ProcessRunning;
            }
        }

        public int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            processId = GetProcessId();
            gameMemoryScanner = new GameMemoryRE7Scanner(processId);
            stopwatch = new Stopwatch();
            stopwatch.Start();
            return 0;
        }

        public int Shutdown()
        {
            gameMemoryScanner?.Dispose();
            gameMemoryScanner = null;
            stopwatch?.Stop();
            stopwatch = null;
            return 0;
        }

        public object PullData()
        {
            try
            {
                if (!get_GameRunning)
                {
                    return null;
                }
                if (!gameMemoryScanner.ProcessRunning)
                {
                    //hostDelegates.Exit();
                    processId = GetProcessId();
                    if (processId != null)
                    {
                        gameMemoryScanner.Initialize(processId.Value); // re-initialize and attempt to continue
                    }
                    else if (!gameMemoryScanner.ProcessRunning)
                    {
                        stopwatch.Restart();
                        return null;
                    }
                }

                if (stopwatch.ElapsedMilliseconds >= 2000L)
                {
                    gameMemoryScanner.UpdatePointers();
                    stopwatch.Restart();
                }
                return gameMemoryScanner.Refresh();
            }
            catch (Exception ex)
            {
                hostDelegates.OutputMessage("[{0}] {1} {2}", ex.GetType().Name, ex.Message, ex.StackTrace);
                return null;
            }
        }

        private int? GetProcessId() => Process.GetProcessesByName("re7")?.FirstOrDefault()?.Id;
    }
}
