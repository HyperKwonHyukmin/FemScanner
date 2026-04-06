using FemScanner.Models;

namespace FemScanner.Models.Elements;

/// <summary>CTETRA 사면체 솔리드 요소 (4노드)</summary>
public class CTetra : IElement
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int[] NodeIds { get; set; } = [];
    public string CardType => "CTETRA";
}
