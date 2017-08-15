using System.Collections.Generic;
using Newtonsoft.Json;

namespace frontend.Controllers
{
    public class TreeViewItem
    {
        [JsonProperty("id", Required = Required.Always)] public int Id;

        [JsonProperty("text", Required = Required.Always)] public string Text;

        [JsonProperty("items")] public List<TreeViewItem> Items;

        // ReSharper disable once UnusedMember.Global
        public bool ShouldSerializeItems() => Items != null && Items.Count > 0;

        [JsonProperty("userdata")] public Dictionary<string, string> Userdata;

        // ReSharper disable once UnusedMember.Global
        public bool ShouldSerializeUserdata() => Userdata != null && Userdata.Count > 0;
    }
}
