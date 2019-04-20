using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BDIv2.App
{
    class PackageJsonModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("main")]
        public string EntryFile { get; set; }
        [JsonProperty("private")]
        public bool isPrivate{ get; set; }
    }
}
