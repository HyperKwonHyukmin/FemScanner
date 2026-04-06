using FemScanner.Models;

namespace FemScanner.Models.Elements;

/// <summary>CTRIA3 삼각형 쉘 요소 (3노드)</summary>
public class CTria3 : IElement
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int[] NodeIds { get; set; } = [];
    public string CardType => "CTRIA3";
}
