namespace DeltGeometriFelleskomponent.Models;

public class BoundsReference
{
    public string Featuretype { get; set; }
    public string LokalId { get; set; }
    public bool Reverse { get; set; }
    public List<int> Idx { get; set; }
}