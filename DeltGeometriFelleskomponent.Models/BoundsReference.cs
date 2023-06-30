using Newtonsoft.Json;

namespace DeltGeometriFelleskomponent.Models;

public class BoundsReference
{
    [JsonProperty("featuretype")]
    public string Featuretype { get; set; }
    
    [JsonProperty("lokalId")]
    public string LokalId { get; set; }

    [JsonProperty("reverse")]
    public bool Reverse { get; set; }
    
    [JsonProperty("idx")]
    public List<int> Idx { get; set; }
}