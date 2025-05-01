using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FructoseLib.IO.Parser
{
    public sealed record InputNode
    {
        public InputNode(string Path)
        {
            try
            {
                if (Path.Length < 3 || !File.Exists(Path) && !Directory.Exists(Path))
                {
                    throw new ArgumentException($"Invalid path to file or directory, please enter correct path. Make sure the path does not contains invalid characters(Non-ASCII symbols), refers to an existing file, and consists entirely of latin characters(Cyrillic may not work on English versions of Windows)");
                }
            }
            catch (ArgumentException) { throw; }
            catch (Exception)
            {
                throw new Exception("An error occurred while reading file or directory. Please run software as administrator and try again");
            }

            this.Path = Path;
            this.Files = new();
            this.Decompressed = new();
        }

        public string Path { get; init; }
        public List<FileNode> Files { get; private set; }
        public List<string> Decompressed { get; private set; }

        public void AddFiles(IEnumerable<FileNode> Files)
        {
            this.Files.AddRange(Files);
            this.Files = new(this.Files.DistinctBy(File => File.Hash));
        }

        public void AddFile(FileNode File)
        {
            this.Files.Add(File);
            this.Files = new(this.Files.DistinctBy(File => File.Hash));
        }

        public void AddDecompressed(IEnumerable<string> Directories)
        {
            this.Decompressed.AddRange(Directories);
        }

        public void AddDecompressed(string Directory)
        {
            this.Decompressed.Add(Directory);
        }
    }
}
