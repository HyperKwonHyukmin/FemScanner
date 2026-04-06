using FemScanner.Models;

namespace FemScanner.Models.Loads;

/// <summary>GRAV 중력 하중 카드</summary>
public class Grav : ILoad
{
    public int Id { get; set; }
    public string CardType => "GRAV";
    public int SubcaseId { get; set; }
    /// <summary>좌표계 ID (CID)</summary>
    public int CoordId { get; set; }
    /// <summary>중력 스케일 팩터 (G)</summary>
    public double Scale { get; set; }
    /// <summary>중력 방향 단위벡터 [N1, N2, N3]</summary>
    public double[] Direction { get; set; } = [0, 0, 0];
}
