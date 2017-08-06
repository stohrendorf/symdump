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
            if (_appState.SymFile == null)
                return null;

            int subId = 0;
            return _appState.SymFile.Labels
                .Select(byAddress => new TreeViewItem
                {
                    Id = (int) byAddress.Key,
                    Text = $"0x{byAddress.Key:x8}",
                    Items = byAddress.Value.Select(lbl => new TreeViewItem
                    {
                        Id = subId++,
                        Text = lbl.Name
                    }).ToList()
                });
        }
    }
}
