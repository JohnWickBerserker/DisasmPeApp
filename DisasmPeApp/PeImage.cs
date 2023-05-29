using DisasmPeApp.Exceptions;
using DisasmPeApp.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisasmPeApp
{
    internal class PeImage
    {
        public PeMachine Machine;
        public ushort NumberOfSections;
        public uint SizeOfCode;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public uint BaseOfData;
        public uint ImageBase;
        public uint SizeOfImage;
        public uint CheckSum;
        public uint NumberOfRvaAndSizes;
        public byte[] Image = new byte[0];

        public List<PeDataDirectory> DataDirectory = new List<PeDataDirectory>();
        public List<PeSection> Sections = new List<PeSection>();
        public List<PeImportDescriptor> ImportDescriptors = new List<PeImportDescriptor>();
        public List<PeExportFunction> ExportFunctions = new List<PeExportFunction>();

        public static PeImage ReadImage(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return ReadImage(stream);
            }
        }

        public static PeImage ReadImage(Stream stream)
        {
            stream.Seek(0x00, SeekOrigin.Begin);
            var magic = stream.ReadUshort();
            if (magic != 0x5a4d)
            {
                throw new NotPeFileException();
            }
            stream.Seek(0x3c, SeekOrigin.Begin);
            var peHeaderPos = stream.ReadUint();

            stream.Seek(peHeaderPos, SeekOrigin.Begin);
            var peMagic = stream.ReadUint();
            if (peMagic != 0x4550)
            {
                throw new NotPeFileException();
            }

            var result = new PeImage();
            result.Machine = (PeMachine)stream.ReadUshort();
            result.NumberOfSections = stream.ReadUshort();
            stream.ReadUint(); // TimeDateStamp
            stream.ReadUint(); // PointerToSymbolTable
            stream.ReadUint(); // NumberOfSymbols
            stream.ReadUshort(); // SizeOfOptionalHeader
            stream.ReadUshort(); // Characteristics
            ReadOptionalHeader(stream, result);
            ReadSections(stream, result);
            using (var memStream = new MemoryStream(result.Image))
            {
                ReadExportDirectory(memStream, result);
                ReadImportDescriptors(memStream, result);
            }
            return result;
        }

        private static void ReadOptionalHeader(Stream stream, PeImage result)
        {
            stream.ReadUshort(); // Magic
            stream.ReadByte(); // MajorLinkerVersion
            stream.ReadByte(); // MinorLinkerVersion
            result.SizeOfCode = stream.ReadUint();
            stream.ReadUint(); // SizeOfInitializedData
            stream.ReadUint(); // SizeOfUninitializedData
            result.AddressOfEntryPoint = stream.ReadUint();
            result.BaseOfCode = stream.ReadUint();
            result.BaseOfData = stream.ReadUint();
            result.ImageBase = stream.ReadUint();
            stream.ReadUint(); // SectionAlignment
            stream.ReadUint(); // FileAlignment
            stream.ReadUshort(); // MajorOperatingSystemVersion
            stream.ReadUshort(); // MinorOperatingSystemVersion
            stream.ReadUshort(); // MajorImageVersion
            stream.ReadUshort(); // MinorImageVersion
            stream.ReadUshort(); // MajorSubsystemVersion
            stream.ReadUshort(); // MinorSubsystemVersion
            stream.ReadUint(); // Win32VersionValue
            result.SizeOfImage = stream.ReadUint();
            stream.ReadUint(); // SizeOfHeaders
            result.CheckSum = stream.ReadUint();
            stream.ReadUshort(); // Subsystem
            stream.ReadUshort(); // DllCharacteristics
            stream.ReadUint(); // SizeOfStackReserve
            stream.ReadUint(); // SizeOfStackCommit
            stream.ReadUint(); // SizeOfHeapReserve
            stream.ReadUint(); // SizeOfHeapCommit
            stream.ReadUint(); // LoaderFlags
            result.NumberOfRvaAndSizes = stream.ReadUint();
            result.DataDirectory = new List<PeDataDirectory>();
            for (var i = 0; i < 16; i++)
            {
                var item = new PeDataDirectory();
                item.Rva = stream.ReadUint();
                item.Size = stream.ReadUint();
                result.DataDirectory.Add(item);
            }
        }

        private static void ReadSections(Stream stream, PeImage result)
        {
            for (var i = 0; i < result.NumberOfSections; i++)
            {
                var section = ReadSection(stream);
                result.Sections.Add(section);
            }

            result.Image = new byte[result.SizeOfImage];
            foreach (var section in result.Sections)
            {
                ReadSectionData(stream, result.Image, section);
            }
        }

        private static PeSection ReadSection(Stream stream)
        {
            var result = new PeSection();
            var nameRaw = new byte[8];
            var ret = stream.Read(nameRaw, 0, 8);
            if (ret != 8)
            {
                throw new NotPeFileException();
            }
            result.Name = StringFromBytes(nameRaw);
            result.RvaLength = stream.ReadUint();
            result.RvaAddress = stream.ReadUint();
            result.RawLength = stream.ReadUint();
            result.RawAddress = stream.ReadUint();
            stream.ReadUint(); // PointerToRelocations
            stream.ReadUint(); // PointerToLinenumbers
            stream.ReadUshort(); // NumberOfRelocations
            stream.ReadUshort(); // NumberOfLinenumbers
            stream.ReadUint(); // Characteristics
            return result;
        }

        private static string StringFromBytes(byte[] str)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] == 0)
                {
                    break;
                }
                sb.Append((char)str[i]);
            }
            return sb.ToString();
        }

        private static void ReadSectionData(Stream stream, byte[] image, PeSection section)
        {
            stream.Seek(section.RawAddress, SeekOrigin.Begin);
            stream.Read(image, (int)section.RvaAddress, (int)Math.Min(section.RvaLength, section.RawLength));
        }

        private static PeImportDescriptor ReadImportDescriptor(Stream stream)
        {
            var result = new PeImportDescriptor();
            result.OriginalFirstThunkRva = stream.ReadUint();
            result.TimeDateStamp = stream.ReadUint();
            result.ForwarderChain = stream.ReadUint();
            result.NameRva = stream.ReadUint();
            result.FirstThunkRva = stream.ReadUint();
            return result;
        }

        private static void ReadImportDescriptors(MemoryStream stream, PeImage result)
        {
            stream.Seek(result.DataDirectory[PeDirectoryConsts.IMAGE_DIRECTORY_ENTRY_IMPORT].Rva, SeekOrigin.Begin);
            var descriptor = ReadImportDescriptor(stream);
            while (!descriptor.IsNull())
            {
                result.ImportDescriptors.Add(descriptor);
                descriptor = ReadImportDescriptor(stream);
            }
            foreach (var item in result.ImportDescriptors)
            {
                stream.Seek(item.NameRva, SeekOrigin.Begin);
                item.Name = stream.ReadString();
                stream.Seek(item.OriginalFirstThunkRva, SeekOrigin.Begin);
                ReadImportFunctions(stream, item);
            }
        }

        private static void ReadImportFunctions(MemoryStream stream, PeImportDescriptor descriptor)
        {
            var rva = stream.ReadUint();
            var functionList = new List<uint>();
            while (rva != 0)
            {
                functionList.Add(rva);
                rva = stream.ReadUint();
            }
            foreach (var functionRva in functionList)
            {
                stream.Seek(functionRva, SeekOrigin.Begin);
                var hint = stream.ReadUshort();
                var name = stream.ReadString();
                descriptor.Functions.Add(new PeImportFunction(hint, name));
            }
        }

        private static void ReadExportDirectory(MemoryStream stream, PeImage result)
        {
            stream.Seek(result.DataDirectory[PeDirectoryConsts.IMAGE_EXPORT_DIRECTORY].Rva, SeekOrigin.Begin);
            stream.ReadUint(); // Characteristics
            stream.ReadUint(); // TimeDateStamp
            stream.ReadUshort(); // MajorVersion
            stream.ReadUshort(); // MinorVersion
            stream.ReadUint(); // Name
            var ordinalBase = stream.ReadUint();
            var numberOfFunctions = stream.ReadUint();
            var numberOfNames = stream.ReadUint();
            var addressOfFunctions = stream.ReadUint();
            var addressOfNames = stream.ReadUint();
            var addressOfNameOrdinals = stream.ReadUint();
            stream.Seek(addressOfFunctions, SeekOrigin.Begin);
            var functionRva = new List<uint>();
            for (var i = 0; i < numberOfFunctions; i++)
            {
                functionRva.Add(stream.ReadUint());
            }
            stream.Seek(addressOfNames, SeekOrigin.Begin);
            var functionNameRva = new List<uint>();
            for (var i = 0; i < numberOfNames; i++)
            {
                functionNameRva.Add(stream.ReadUint());
            }
            stream.Seek(addressOfNameOrdinals, SeekOrigin.Begin);
            var functionOrdinal = new List<ushort>();
            for (var i = 0; i < numberOfNames; i++)
            {
                functionOrdinal.Add(stream.ReadUshort());
            }
            for (var i = 0; i < functionRva.Count; i++)
            {
                if (i < functionNameRva.Count)
                {
                    stream.Seek(functionNameRva[i], SeekOrigin.Begin);
                    var name = stream.ReadString();
                    result.ExportFunctions.Add(
                        new PeExportFunction(functionRva[i], name, (ushort)(ordinalBase + functionOrdinal[i])));
                }
                else
                {
                    result.ExportFunctions.Add(new PeExportFunction(functionRva[i]));
                }
            }
            result.ExportFunctions = result.ExportFunctions.OrderBy(x => x.Ordinal).ThenBy(x => x.Name).ToList();
        }

        public PeImportFunction GetImportFunction(uint rva)
        {
            foreach (var d in ImportDescriptors)
            {
                var funcIndex = (rva - d.FirstThunkRva - ImageBase) / 4;
                if (funcIndex >= 0 && funcIndex < d.Functions.Count)
                {
                    return d.Functions[(int)funcIndex];
                }
            }
            return new PeImportFunction(0, "not_found");
        }
    }
}
