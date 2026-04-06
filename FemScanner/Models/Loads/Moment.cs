using FemScanner.Models;

namespace FemScanner.Models.Loads;

/// <summary>MOMENT 집중 모멘트 카드</summary>
public class Moment : ILoad
{
    public int Id { get; set; }
    public int SubcaseId { get; set; }
    public string CardType => "MOMENT";
    public int NodeId { get; set; }
    public int CoordId { get; set; }
    public double Magnitude { get; set; }
    public double[] Direction { get; set; } = new double[3];
}
