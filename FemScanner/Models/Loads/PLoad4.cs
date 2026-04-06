using FemScanner.Models;

namespace FemScanner.Models.Loads;

/// <summary>PLOAD4 요소면 압력 하중 카드</summary>
public class PLoad4 : ILoad
{
    public int Id { get; set; }
    public int SubcaseId { get; set; }
    public string CardType => "PLOAD4";
    public int ElementId { get; set; }
    public double Pressure { get; set; }
}
