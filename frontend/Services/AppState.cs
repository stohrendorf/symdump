using System.Diagnostics;
using System.IO;
using System.Linq;
using core.util;
using exefile;
using frontend.Models;
using Microsoft.EntityFrameworkCore;
using symfile;

namespace frontend.Services
{
    public class AppState
    {
        public SymFile SymFile;
        public ExeFile ExeFile;

        public AppState()
        {
            using (var db = new Context())
            {
                Debug.Assert(db.Projects != null);

                if (!db.Projects.Any())
                    return;
                
                var project = db.Projects.Include(p => p.Exe).Include(p => p.Sym).First();
                if (project.Sym == null)
                    return;
                
                SymFile = new SymFile(new BinaryReader(new MemoryStream(project.Sym.Data)));

                if (project.Exe != null)
                {
                    ExeFile = new ExeFile(new EndianBinaryReader(new MemoryStream(project.Exe.Data)), SymFile);
                    ExeFile.Disassemble();
                }
            }
        }
    }
}
