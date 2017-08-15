using System.Collections.Generic;
using Newtonsoft.Json;

namespace frontend.Controllers
{
    public class TreeViewItem
    {
        [JsonProperty("id", Required = Required.Always)] public int Id;

        [JsonProperty("text", Required = Required.Always)] public string Text;

        [JsonProperty("items")] public List<TreeViewItem> Items;

        public bool ShouldSerializeItems() => Items != null && Items.Count > 0;

        [JsonProperty("userdata", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Userdata;
        
        public bool ShouldSerializeUserdata() => Userdata != null && Userdata.Count > 0;
    }
}
