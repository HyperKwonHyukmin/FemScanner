using FemScanner.Models;

namespace FemScanner.Models.Properties;

/// <summary>PBEAM 보 속성 카드</summary>
public class PBeam : IProperty
{
    public int Id { get; set; }
    public string CardType => "PBEAM";
    public int MaterialId { get; set; }
    public double A { get; set; }
    public double I1 { get; set; }
    public double I2 { get; set; }
    public double J { get; set; }
}
