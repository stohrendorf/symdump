using System.IO;
using core.util;
using exefile;
using symfile;

namespace symdump
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            SymFile symFile;
            using (var fs = new FileStream(args[0], FileMode.Open))
            {
                symFile = new SymFile(new BinaryReader(fs));
                //symFile.dump(Console.Out);
            }

            var exeFilename = Path.ChangeExtension(args[0], "EXE");

            if (!File.Exists(exeFilename))
                return;

            using (var fs = new EndianBinaryReader(new FileStream(exeFilename, FileMode.Open)))
            {
                var exeFile = new ExeFile(fs, symFile);
                exeFile.disassemble();
                exeFile.decompile();
            }
        }
    }
}
