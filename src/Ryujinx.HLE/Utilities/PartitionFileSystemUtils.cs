using LibHac;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using Ryujinx.HLE.FileSystem;
using System.IO;

namespace Ryujinx.HLE.Utilities
{
    public static class PartitionFileSystemUtils
    {
        public static IFileSystem OpenApplicationFileSystem(string path, VirtualFileSystem fileSystem, bool throwOnFailure = true)
        {
            FileStream file = File.OpenRead(path);

            return OpenApplicationFileSystem(file, Path.GetExtension(path).ToLower() == ".xci", fileSystem, throwOnFailure);
        }

        public static IFileSystem OpenApplicationFileSystem(Stream stream, bool isXci, VirtualFileSystem fileSystem, bool throwOnFailure = true)
        {
            IFileSystem partitionFileSystem;

            if (isXci)
            {
                partitionFileSystem = new Xci(fileSystem.KeySet, stream.AsStorage()).OpenPartition(XciPartitionType.Secure);
            }
            else
            {
                var pfsTemp = new PartitionFileSystem();
                Result initResult = pfsTemp.Initialize(stream.AsStorage());

                if (throwOnFailure)
                {
                    initResult.ThrowIfFailure();
                }
                else if (initResult.IsFailure())
                {
                    return null;
                }

                partitionFileSystem = pfsTemp;
            }

            fileSystem.ImportTickets(partitionFileSystem);

            return partitionFileSystem;
        }
    }
}
