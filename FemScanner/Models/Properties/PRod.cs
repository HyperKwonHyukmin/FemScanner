using FemScanner.Models;

namespace FemScanner.Models.Properties;

/// <summary>PROD 로드 속성 카드</summary>
public class PRod : IProperty
{
    public int Id { get; set; }
    public string CardType => "PROD";
    public int MaterialId { get; set; }
    public double A { get; set; }
    public double J { get; set; }
}
