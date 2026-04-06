using FemScanner.Models;

namespace FemScanner.Models.Loads;

/// <summary>FORCE 집중하중 카드</summary>
public class Force : ILoad
{
    public int Id { get; set; }
    public int SubcaseId { get; set; }
    public string CardType => "FORCE";
    public int NodeId { get; set; }
    public int CoordId { get; set; }
    public double Magnitude { get; set; }
    public double[] Direction { get; set; } = new double[3];
}
