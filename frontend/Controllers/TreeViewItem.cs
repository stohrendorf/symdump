using System.Collections.Generic;
using Newtonsoft.Json;

namespace frontend.Controllers
{
    public class TreeViewItem
    {
        [JsonProperty("id", Required = Required.Always)]
        public int Id;

        [JsonProperty("items")] public List<TreeViewItem> Items;

        [JsonProperty("text", Required = Required.Always)]
        public string Text;

        [JsonProperty("userdata")] public Dictionary<string, string> Userdata;

        // ReSharper disable once UnusedMember.Global
        public bool ShouldSerializeItems()
        {
            return Items != null && Items.Count > 0;
        }

        // ReSharper disable once UnusedMember.Global
        public bool ShouldSerializeUserdata()
        {
            return Userdata != null && Userdata.Count > 0;
        }
    }
}