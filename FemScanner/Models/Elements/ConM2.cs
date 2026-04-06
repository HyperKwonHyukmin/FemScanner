using FemScanner.Models;

namespace FemScanner.Models.Elements;

/// <summary>CONM2 집중 질량 요소</summary>
public class ConM2 : IElement
{
    public int Id { get; set; }
    public string CardType => "CONM2";
    /// <summary>PropertyId 없는 카드 — 항상 0</summary>
    public int PropertyId => 0;
    /// <summary>질량이 부착된 절점 ID (G)</summary>
    public int NodeId { get; set; }
    /// <summary>좌표계 ID (CID)</summary>
    public int CoordId { get; set; }
    /// <summary>집중 질량값 (M)</summary>
    public double Mass { get; set; }
    /// <summary>절점 오프셋 [X1, X2, X3]</summary>
    public double[] Offset { get; set; } = [0, 0, 0];
    public int[] NodeIds => [NodeId];
}
