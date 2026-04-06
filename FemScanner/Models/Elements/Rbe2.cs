using FemScanner.Models;

namespace FemScanner.Models.Elements;

/// <summary>RBE2 강체 요소 (독립 노드 1개 → 종속 노드 N개)</summary>
public class Rbe2 : IElement
{
    public int Id { get; set; }
    public string CardType => "RBE2";
    /// <summary>PropertyId 없는 카드 — 항상 0</summary>
    public int PropertyId => 0;
    /// <summary>독립 노드 ID (GN)</summary>
    public int IndependentNodeId { get; set; }
    /// <summary>구속 자유도 (CM, 예: "123456")</summary>
    public string ConstraintDofs { get; set; } = string.Empty;
    /// <summary>종속 노드 ID 배열 (GM1, GM2, ...)</summary>
    public int[] DependentNodeIds { get; set; } = [];
    /// <summary>모든 연결 노드 (GN + GM*)</summary>
    public int[] NodeIds => [IndependentNodeId, .. DependentNodeIds];
}
