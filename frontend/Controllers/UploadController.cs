using System.IO;
using core.util;
using exefile;
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
            _appState.SymFile = new SymFile(new BinaryReader(file.OpenReadStream()));
        }

        [HttpPost("exe")]
        public void Exe(IFormFile file)
        {
            _appState.ExeFile = new ExeFile(new EndianBinaryReader(file.OpenReadStream()), _appState.SymFile);
            _appState.ExeFile.Disassemble();
        }
    }
}
