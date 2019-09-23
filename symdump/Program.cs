using System;
using System.IO;
using symdump.symfile;

namespace symdump
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            SymFile symFile;
            using (var fs = new FileStream(args[0], FileMode.Open))
            {
                symFile = new SymFile(new BinaryReader(fs), false);
                symFile.Dump(Console.Out);
            }

            var exeFilename = Path.ChangeExtension(args[0], "EXE");

            if (!File.Exists(exeFilename))
                return;

#if false
            using (var fs = new EndianBinaryReader(new FileStream(exeFilename, FileMode.Open)))
            {
                var exeFile = new ExeFile(fs, symFile);
                exeFile.Disassemble();
            }
#endif
        }
    }
}