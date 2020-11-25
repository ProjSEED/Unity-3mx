using System.Collections.Generic;

namespace Unity3MX.Schema
{
    #pragma warning disable // Disable all warnings
    
    public partial class Node
    {
        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Always)]
        public string Id { get; set; }

        /// <summary>An array of 3 numbers that define an min of the bounding box.</summary>
        [Newtonsoft.Json.JsonProperty("bbMin", Required = Newtonsoft.Json.Required.Always)]
        public List<float> BBMin = new List<float>();

        /// <summary>An array of 3 numbers that define an max of the bounding box.</summary>
        [Newtonsoft.Json.JsonProperty("bbMax", Required = Newtonsoft.Json.Required.Always)]
        public List<float> BBMax = new List<float>();

        [Newtonsoft.Json.JsonProperty("maxScreenDiameter", Required = Newtonsoft.Json.Required.Always)]
        public float MaxScreenDiameter { get; set; }

        [Newtonsoft.Json.JsonProperty("children", Required = Newtonsoft.Json.Required.Always)]
        public List<string> Children = new List<string>();

        [Newtonsoft.Json.JsonProperty("resources", Required = Newtonsoft.Json.Required.Always)]
        public List<string> Resources = new List<string>();

        public string ToJson() 
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
        
        public static Node FromJson(string data)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Node>(data);
        }
    }
    
    public partial class Resource
    {
        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Always)]
        public string Id { get; set; }

        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string Type { get; set; }

        [Newtonsoft.Json.JsonProperty("format", Required = Newtonsoft.Json.Required.Always)]
        public string Format { get; set; }

        [Newtonsoft.Json.JsonProperty("size", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Size { get; set; }

        [Newtonsoft.Json.JsonProperty("file", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string File { get; set; }

        /// <summary>An array of 3 numbers that define an min of the bounding box.</summary>
        [Newtonsoft.Json.JsonProperty("bbMin", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public List<float> BBMin = new List<float>();

        /// <summary>An array of 3 numbers that define an max of the bounding box.</summary>
        [Newtonsoft.Json.JsonProperty("bbMax", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public List<float> BBMax = new List<float>();

        [Newtonsoft.Json.JsonProperty("texture", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Texture { get; set; }   
    
        public string ToJson() 
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
        
        public static Resource FromJson(string data)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Resource>(data);
        }
    }
    
    public partial class Header3MXB 
    {
        [Newtonsoft.Json.JsonProperty("version", Required = Newtonsoft.Json.Required.Always)]
        public int Version { get; set; }
    
        [Newtonsoft.Json.JsonProperty("nodes", Required = Newtonsoft.Json.Required.Always)]
        public List<Node> Nodes = new List<Node>();

        [Newtonsoft.Json.JsonProperty("resources", Required = Newtonsoft.Json.Required.Always)]
        public List<Resource> Resources = new List<Resource>();
    
        public string ToJson() 
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
        
        public static Header3MXB FromJson(string data)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Header3MXB>(data);
        }
    }

    public partial class _3mxLayer
    {
        [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Always)]
        public string Name { get; set; }

        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Always)]
        public string Id { get; set; }

        [Newtonsoft.Json.JsonProperty("root", Required = Newtonsoft.Json.Required.Always)]
        public string Root { get; set; }

        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public static Header3MXB FromJson(string data)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Header3MXB>(data);
        }
    }

    public partial class _3mx
    {
        [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Always)]
        public string Name { get; set; }

        [Newtonsoft.Json.JsonProperty("layers", Required = Newtonsoft.Json.Required.Always)]
        public List<_3mxLayer> Layers = new List<_3mxLayer>();

        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public static Header3MXB FromJson(string data)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Header3MXB>(data);
        }
    }


}