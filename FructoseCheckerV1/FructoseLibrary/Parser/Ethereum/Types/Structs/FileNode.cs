using System;
using System.Collections.Concurrent;
using System.IO;

using FructoseLib.Types.Ethereum.Enums;

namespace FructoseLib.IO.Parser
{
    public sealed record FileNode
    {
        public FileNode(string Path)
        {
            this.Path = Path;
            this.Finded = new();
            this.InvalidFinded = new();
            this.Hash = GetHash(this.Path);
            this.Size = GetSize(this.Path);
        }

        public string Path { get; init; }
        public string Hash { get; init; }
        public long Size { get; init; }
        private static string GetHash(string Path)
        {
            using var MD5 = System.Security.Cryptography.MD5.Create();
            using var Stream = System.IO.File.OpenRead(Path);
            return Convert.ToHexString(MD5.ComputeHash(Stream));
        }
        
        private static long GetSize(string Path)
        {
            return new FileInfo(Path).Length;
        }

        public ConcurrentDictionary<EthereumUnitTypeCategory, long> Finded { get; init; }

        public ConcurrentDictionary<EthereumUnitTypeCategory, long> InvalidFinded { get; init; }
    }
}
