using System;
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

        [HttpGet("instructions/{offset}")]
        public IEnumerable<LineInfo> Instructions([FromRoute] uint offset)
        {
            offset = _appState.ExeFile.MakeLocal(offset);
            var firstAddr = _appState.ExeFile.Instructions.Values
                .Where(kv => kv.Address >= offset)
                .OrderBy(kv => kv.Address)
                .First().Address;
            
            var q = new Queue<uint>();
            q.Enqueue(firstAddr);

            var blocks = new SortedDictionary<uint, MicroAssemblyBlock>();

            while (q.Count > 0)
            {
                var addr = q.Dequeue();
                if(blocks.ContainsKey(addr))
                    continue;
                
                var block = _appState.ExeFile.BlockAtLocal(addr);
                if(block == null)
                    continue;
                
                blocks[addr] = block;

                foreach (var o in block.Outs)
                {
                    switch (o.Value)
                    {
                        case JumpType.Call:
                        case JumpType.CallConditional:
                            break;
                        case JumpType.Jump:
                        case JumpType.JumpConditional:
                        case JumpType.Control:
                            q.Enqueue(o.Key);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            
            int i = 0;
            return blocks
                .SelectMany(kv => kv.Value.Insns.Select(insn => new KeyValuePair<uint, MicroInsn>(kv.Key, insn)))
                .Select(kv => new LineInfo
                {
                    Text = kv.Value.ToString(),
                    Address = $"{_appState.ExeFile.MakeGlobal(kv.Key)}/{i++}"
                });
        }
    }
}
