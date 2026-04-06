using FemScanner.Models;

namespace FemScanner.Models.Elements;

/// <summary>CROD 로드 요소 (2노드)</summary>
public class CRod : IElement
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int[] NodeIds { get; set; } = [];
    public string CardType => "CROD";
}
