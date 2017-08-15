using System.IO;
using System.Linq;
using core.util;
using exefile;
using frontend.Models;
using frontend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using symfile;

namespace frontend.Controllers
{
    [Route("api/upload")]
    public class UploadController
    {
        private readonly AppState _appState;

        public UploadController(AppState appState)
        {
            _appState = appState;
        }

        [HttpPost("sym")]
        public void Sym(IFormFile file)
        {
            using (var db = new Context())
            {
                if (!db.Projects.Any(x => true))
                {
                    db.Projects.Add(new Project());
                    db.SaveChanges();
                }

                var project = db.Projects.First();
                project.Exe = null;
                project.Sym = new BinaryFile
                {
                    Name = file.FileName,
                    Data = new BinaryReader(file.OpenReadStream()).ReadBytes((int) file.Length)
                };
                db.SaveChanges();
                
                _appState.SymFile = new SymFile(new BinaryReader(new MemoryStream(project.Sym.Data)));
            }
        }

        [HttpPost("exe")]
        public void Exe(IFormFile file)
        {
            using (var db = new Context())
            {
                if (!db.Projects.Any(x => true) || _appState.SymFile == null)
                {
                    return;
                }
                
                var project = db.Projects.First();
                project.Exe = new BinaryFile
                {
                    Name = file.FileName,
                    Data = new BinaryReader(file.OpenReadStream()).ReadBytes((int) file.Length)
                };
                db.SaveChanges();

                _appState.ExeFile = new ExeFile(new EndianBinaryReader(new MemoryStream(project.Exe.Data)), _appState.SymFile);
                _appState.ExeFile.Disassemble();
            }
        }
    }
}
