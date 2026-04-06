using FemScanner.Models;

namespace FemScanner.Models.Elements;

/// <summary>CHEXA 육면체 솔리드 요소 (8노드)</summary>
public class CHexa : IElement
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int[] NodeIds { get; set; } = [];
    public string CardType => "CHEXA";
}
