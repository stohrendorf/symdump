using System.Collections.Generic;
using Newtonsoft.Json;

namespace frontend.Controllers
{
    public class TreeViewItem
    {
        [JsonProperty("id")]
        public int Id;
        
        [JsonProperty("text")]
        public string Text;
        
        [JsonProperty("items")]
        public List<TreeViewItem> Items;
        
        [JsonProperty("userdata")]
        public Dictionary<string, string> Userdata = new Dictionary<string, string>();
    }
}
