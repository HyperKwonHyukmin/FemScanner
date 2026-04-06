using FemScanner.Models;

namespace FemScanner.Models.Elements;

/// <summary>CQUAD4 사각형 쉘 요소 (4노드)</summary>
public class CQuad4 : IElement
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int[] NodeIds { get; set; } = [];
    public string CardType => "CQUAD4";
}
