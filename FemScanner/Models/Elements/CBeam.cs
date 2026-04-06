using FemScanner.Models;

namespace FemScanner.Models.Elements;

/// <summary>CBEAM 보 요소 (2노드 + 방향벡터)</summary>
public class CBeam : IElement
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int[] NodeIds { get; set; } = [];
    public string CardType => "CBEAM";

    /// <summary>방향 참조 그리드 ID (G0 방식)</summary>
    public int G0 { get; set; }

    /// <summary>방향벡터 (X1, X2, X3 방식, G0가 0일 때 사용)</summary>
    public double[] X { get; set; } = new double[3];
}
