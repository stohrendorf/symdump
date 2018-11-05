using System.Collections.Generic;
using System.Linq;
using core.microcode;
using frontend.Services;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace frontend.Controllers
{
    [Route("api/assembly")]
    public class AssemblyController : Controller
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly AppState _appState;

        public AssemblyController(AppState appState)
        {
            _appState = appState;
        }

        [HttpGet("instructions/{offset}/{length}")]
        public IEnumerable<LineInfo> Instructions([FromRoute] int offset, [FromRoute] int length)
        {
            int i = 0;
            return _appState.ExeFile.RelocatedInstructions
                .Where(kv => kv.Key >= offset)
                .OrderBy(kv => kv.Key)
                .Take(length)
                .SelectMany(kv => kv.Value.Insns.Select(insn => new KeyValuePair<uint, MicroInsn>(kv.Key, insn)))
                .Select(kv => new LineInfo
                {
                    Text = kv.Value.ToString(),
                    Address = $"{kv.Key}/{i++}"
                });
        }
    }
}
