using System.Collections;
using System.Collections.Generic;
using System.Linq;
using frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    [Route("api/assembly")]
    public class AssemblyController : Controller
    {
        private readonly AppState _appState;

        public AssemblyController(AppState appState)
        {
            _appState = appState;
        }

        [HttpGet("instructions/{offset}/{length}")]
        public string Instructions([FromRoute] int offset, [FromRoute] int length)
        {
            return string.Join("\n", _appState.ExeFile.Instructions
                .Where(kv => kv.Key >= offset)
                .OrderBy(kv => kv.Key)
                .Take(length)
                .Select(kv => $"0x{kv.Key:x8}    {kv.Value.AsReadable()}"));
        }
    }
}
