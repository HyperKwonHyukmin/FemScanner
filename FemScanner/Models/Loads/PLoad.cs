using FemScanner.Models;

namespace FemScanner.Models.Loads;

/// <summary>PLOAD 면압 하중 카드</summary>
public class PLoad : ILoad
{
    public int Id { get; set; }
    public int SubcaseId { get; set; }
    public string CardType => "PLOAD";
    public double Pressure { get; set; }
    public int[] ElementIds { get; set; } = [];
}
