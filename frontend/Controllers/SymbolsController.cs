using System.Collections.Generic;
using System.Linq;
using core;
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
            return _appState.SymFile?.Labels
                .Select(byAddress => new TreeViewItem
                {
                    Id = (int) byAddress.Key,
                    Text = $"0x{byAddress.Key:x8}",
                    Items = byAddress.Value.Select(lbl => new TreeViewItem
                    {
                        Id = (int) byAddress.Key,
                        Text = lbl.Name
                    }).ToList()
                });
        }
    }
}
