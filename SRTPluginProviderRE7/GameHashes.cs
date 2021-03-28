using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SRTPluginProviderRE7
{
    /// <summary>
    /// SHA256 hashes for the RE3/BIO3 REmake game executables.
    /// 
    /// Resident Evil 3 (WW): https://steamdb.info/app/952060/ / https://steamdb.info/depot/952062/
    /// Biohazard 3 (CERO Z): https://steamdb.info/app/1100830/ / https://steamdb.info/depot/1100831/
    /// </summary>
    /// 

    public enum GameVersion 
    {
        STEAM,
        WINDOWS,
        UNKNOWN
    }

    public static class GameHashes
    {
        private static readonly byte[] steam_version = new byte[32] { 56, 234, 231, 153, 211, 138, 170, 140, 77, 116, 45, 186, 220, 25, 12, 27, 20, 129, 97, 119, 88, 107, 43, 8, 143, 30, 147, 159, 71, 204, 93, 112 };

        public static GameVersion DetectVersion(string filePath)
        {
            if (filePath.Contains("Windows"))
            {
                return GameVersion.WINDOWS;
            }

            byte[] checksum;
            using (SHA256 hashFunc = SHA256.Create())
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                checksum = hashFunc.ComputeHash(fs);

            if (checksum.SequenceEqual(steam_version))
                return GameVersion.STEAM;
            else
                return GameVersion.UNKNOWN;
        }
    }
}