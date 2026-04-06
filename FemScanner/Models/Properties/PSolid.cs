using FemScanner.Models;

namespace FemScanner.Models.Properties;

/// <summary>PSOLID 솔리드 속성 카드</summary>
public class PSolid : IProperty
{
    public int Id { get; set; }
    public string CardType => "PSOLID";
    public int MaterialId { get; set; }
}
