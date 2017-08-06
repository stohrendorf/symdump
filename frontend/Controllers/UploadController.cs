using System.IO;
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
    }
}
