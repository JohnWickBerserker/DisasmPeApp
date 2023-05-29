using DisasmPeApp.Commands;
using SharpDisasm;
using SharpDisasm.Udis86;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DisasmPeApp.ViewModels
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private ICommand? _clickCommand;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand OpenFileCommand
        {
            get
            {
                return _clickCommand ?? (_clickCommand = new CommandHandler(() => OpenFile()));
            }
        }

        public List<SubViewModel> SubList { get; set; } = new List<SubViewModel>();
        public string? DisasmText { get; set; }

        public void OpenFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Executable";
            dialog.DefaultExt = "*.exe,*.dll";
            dialog.Filter = "Executable (.exe .dll)|*.exe;*.dll";
            if (dialog.ShowDialog() == true)
            {
                var filename = dialog.FileName;
                var image = PeImage.ReadImage(filename);
                var analyzed = AnalyzeImage(image);
                var text = ImageToText(analyzed, out var subList);
                DisasmText = text;
                SubList = subList;
            }
        }

        private string ImageToText(AnalyzedImage analyzed, out List<SubViewModel> subList)
        {
            subList = new List<SubViewModel>(); 
            Disassembler.Translator.IncludeAddress = true;
            Disassembler.Translator.IncludeBinary = true;

            var nlCount = 0;
            var sb = new StringBuilder();
            var currentByte = 0;
            foreach (var i in analyzed.Instructions)
            {
                PutBytes(sb, analyzed.Image.Image, (int)currentByte, (int)((uint)i.Value.Offset - currentByte), ref nlCount);
                currentByte = (int)i.Value.Offset;
                if (analyzed.Subs.ContainsKey(i.Key))
                {
                    PutSubInfo(sb, analyzed.Subs[i.Key], ref nlCount);
                    subList.Add(new SubViewModel { Name = analyzed.Subs[i.Key], Rva = i.Key, Line = nlCount-1 });
                }
                PutInstruction(sb, i.Value, analyzed, ref nlCount);
                currentByte = (int)i.Value.Offset + i.Value.Length;
            }

            return sb.ToString();
        }

        private void PutBytes(StringBuilder sb, byte[] data, int startIndex, int length, ref int nlCount)
        {
            if (length == 0)
            {
                return;
            }
            var firstLength = Math.Min(length, 16 - startIndex % 16);
            PutLineOfBytes(sb, data, startIndex, firstLength);
            nlCount++;
            length -= firstLength;
            startIndex += firstLength;
            if (length == 0)
            {
                return;
            }
            while (length >= 16)
            {
                PutLineOfBytes(sb, data, startIndex, 16);
                nlCount++;
                length -= 16;
                startIndex += 16;
            }
            if (length == 0)
            {
                return;
            }
            var lastLength = length;
            PutLineOfBytes(sb, data, startIndex, lastLength);
            nlCount++;
        }

        private void PutLineOfBytes(StringBuilder sb, byte[] data, int startIndex, int length)
        {
            sb.Append(startIndex.ToString("X8").ToLower() + " ");
            for (var i = 0; i < startIndex % 16; i++)
            {
                sb.Append("   ");
            }
            for (var i = 0; i < length; i++)
            {
                sb.Append(data[startIndex + i].ToString("X2").ToLower());
                sb.Append(" ");
            }
            sb.Append(Environment.NewLine);
        }

        private void PutSubInfo(StringBuilder sb, string subInfo, ref int nlCount)
        {
            sb.Append("// *** ");
            sb.Append(subInfo);
            sb.Append(" ***");
            sb.Append(Environment.NewLine);
            nlCount++;
        }

        private void PutInstruction(StringBuilder sb, Instruction instruction, AnalyzedImage analyzed, ref int nlCount)
        {
            var instructionAppended = false;
            if (instruction.Mnemonic == ud_mnemonic_code.UD_Icall &&
                instruction.Operands[0].Base == ud_type.UD_NONE)
            {
                if (instruction.Operands[0].Type == ud_type.UD_OP_MEM)
                {
                    var importRva = analyzed.Image.ImageBase +
                        analyzed.Image.DataDirectory[PeDirectoryConsts.IMAGE_DIRECTORY_ENTRY_IMPORT].Rva;
                    if (instruction.Operands[0].Value >= importRva)
                    {
                        sb.Append(instruction.Offset.ToString("X8") + " ");
                        AppendBytes(sb, instruction.Bytes);
                        AppendSpaces(sb, (10 - instruction.Bytes?.Length ?? 0) * 3);
                        sb.Append(" call import function " + analyzed.Image.GetImportFunction((uint)instruction.Operands[0].Value));
                        instructionAppended = true;
                    }
                }
                else if (instruction.Operands[0].Type == ud_type.UD_OP_JIMM)
                {
                    var callAddress = (uint)((long)instruction.PC + instruction.Operands[0].Value);
                    if (analyzed.Instructions.ContainsKey(callAddress))
                    {
                        var calledInstruction = analyzed.Instructions[callAddress];
                        if (calledInstruction.Mnemonic == ud_mnemonic_code.UD_Ijmp &&
                            calledInstruction.Operands[0].Base == ud_type.UD_NONE &&
                            calledInstruction.Operands[0].Type == ud_type.UD_OP_MEM)
                        {
                            var importRva = analyzed.Image.ImageBase +
                                analyzed.Image.DataDirectory[PeDirectoryConsts.IMAGE_DIRECTORY_ENTRY_IMPORT].Rva;
                            if (calledInstruction.Operands[0].Value >= importRva)
                            {
                                sb.Append(instruction.Offset.ToString("X8") + " ");
                                AppendBytes(sb, instruction.Bytes);
                                AppendSpaces(sb, (10 - instruction.Bytes?.Length ?? 0) * 3);
                                sb.Append(" call jmp import function " + analyzed.Image.GetImportFunction((uint)calledInstruction.Operands[0].Value));
                                instructionAppended = true;
                            }
                        }
                    }
                }
            }
            if (!instructionAppended)
            {
                sb.Append(instruction.ToString());
            }
            sb.Append(Environment.NewLine);
            nlCount++;
        }

        private void AppendBytes(StringBuilder sb, byte[] bytes)
        {
            if (bytes != null)
            {
                foreach (var b in bytes)
                {
                    sb.Append(b.ToString("X2"));
                    sb.Append(" ");
                }
            }
        }

        private void AppendSpaces(StringBuilder sb, int length)
        {
            for (var i = 0; i < length; i++)
            {
                sb.Append(' ');
            }
        }

        private AnalyzedImage AnalyzeImage(PeImage image)
        {
            var processed = new HashSet<uint>();
            var instructions = new SortedDictionary<uint, Instruction>();
            var subs = new SortedDictionary<uint, string>();
            var toProcess = new Stack<uint>();

            foreach (var i in image.ExportFunctions)
            {
                toProcess.Push(i.FunctionRva);
                subs.Add(i.FunctionRva, i.Name ?? "sub_" + i.FunctionRva.ToString("X8"));
            }
            toProcess.Push(image.AddressOfEntryPoint);
            subs.Add(image.AddressOfEntryPoint, "EntryPoint");

            toProcess.Push(image.BaseOfCode);
            subs.Add(image.BaseOfCode, "BaseOfCode");

            while (toProcess.Count > 0)
            {
                var address = toProcess.Pop();
                if (processed.Contains(address))
                {
                    continue;
                }
                var instruction = DisasmHelper.Disasm(image.Image, address, address);
                if (instruction != null && !instruction.Error)
                {
                    instructions.Add(address, instruction);
                }
                if (instruction != null && !instruction.Error)
                {
                    if (IsReturnInstruction(instruction))
                    {
                    }
                    else if (IsNonCondJump(instruction))
                    {
                        var op = instruction.Operands[0];
                        if (op.Base == ud_type.UD_NONE && op.Type == ud_type.UD_OP_JIMM)
                        {
                            toProcess.Push((uint)op.Value + (uint)instruction.PC);
                        }
                    }
                    else if (IsCondJump(instruction))
                    {
                        toProcess.Push((uint)instruction.PC);

                        var op = instruction.Operands[0];
                        if (op.Base == ud_type.UD_NONE && op.Type == ud_type.UD_OP_JIMM)
                        {
                            toProcess.Push((uint)op.Value + (uint)instruction.PC);
                        }
                    }
                    else if (IsCallInstruction(instruction))
                    {
                        toProcess.Push((uint)instruction.PC);

                        var op = instruction.Operands[0];
                        if (op.Base == ud_type.UD_NONE && op.Type == ud_type.UD_OP_JIMM)
                        {
                            var callAddress = (uint)op.Value + (uint)instruction.PC;
                            toProcess.Push(callAddress);
                            if (!subs.ContainsKey(callAddress))
                            {
                                subs.Add(callAddress, "sub_" + callAddress.ToString("X8"));
                            }
                        }
                    }
                    else
                    {
                        toProcess.Push((uint)instruction.PC);
                    }
                }
                processed.Add(address);
            }
            return new AnalyzedImage
            {
                Image = image,
                Instructions = instructions,
                Subs = subs
            };
        }

        private bool IsCallInstruction(Instruction instruction)
        {
            return instruction.Mnemonic == ud_mnemonic_code.UD_Icall;
        }

        private bool IsNonCondJump(Instruction instruction)
        {
            return instruction.Mnemonic == ud_mnemonic_code.UD_Ijmp;
        }

        private bool IsCondJump(Instruction instruction)
        {
            return
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijg ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijge ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijl ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijle ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijz ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijnz ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijcxz ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijecxz ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijrcxz ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijo ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijno ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijs ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijns ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijp ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijnp ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ija ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijae ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijb ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Ijbe;
        }

        private bool IsReturnInstruction(Instruction instruction)
        {
            return
                instruction.Mnemonic == ud_mnemonic_code.UD_Iret ||
                instruction.Mnemonic == ud_mnemonic_code.UD_Iretf;
        }
    }
}
