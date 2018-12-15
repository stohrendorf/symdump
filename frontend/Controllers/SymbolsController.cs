using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    [Route("api/symbols")]
    public class SymbolsController : Controller
    {
        private readonly AppState _appState;

        public SymbolsController(AppState appState)
        {
            _appState = appState;
        }

        [HttpGet]
        public IEnumerable<TreeViewItem> Get()
        {
            var id = 0;
            return _appState.SymFile?.Labels
                .Select(byAddress => new TreeViewItem
                {
                    Id = id++,
                    Text = $"0x{byAddress.Key:x8}",
                    Userdata = new Dictionary<string, string> {{"address", byAddress.Key.ToString()}},
                    Items = byAddress.Value.Select(lbl => new TreeViewItem
                    {
                        Id = id++,
                        Text = lbl.Name,
                        Userdata = new Dictionary<string, string> {{"address", byAddress.Key.ToString()}}
                    }).ToList()
                });
        }

        [HttpGet("callees")]
        public IEnumerable<TreeViewItem> Callees()
        {
            return _appState.PSXExeFile?.TextSection.CalleesBySource.Values.SelectMany(x => x).ToImmutableSortedSet()
                .Select(address =>
                    new TreeViewItem
                    {
                        Id = (int) address,
                        Text = $"0x{address:x8} " + _appState.SymFile.GetSymbolName(address),
                        Userdata = new Dictionary<string, string> {{"address", address.ToString()}}
                    });
        }
    }
}