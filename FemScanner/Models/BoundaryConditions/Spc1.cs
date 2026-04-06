using FemScanner.Models;

namespace FemScanner.Models.BoundaryConditions;

/// <summary>SPC1 다중 노드 구속 카드</summary>
public class Spc1 : IBoundaryCondition
{
    public int Id { get; set; }
    public string CardType => "SPC1";
    public string Dof { get; set; } = string.Empty;
    public int[] NodeIds { get; set; } = [];
}
