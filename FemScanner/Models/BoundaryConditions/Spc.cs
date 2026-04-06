using FemScanner.Models;

namespace FemScanner.Models.BoundaryConditions;

/// <summary>SPC 단일점 구속 카드</summary>
public class Spc : IBoundaryCondition
{
    public int Id { get; set; }
    public string CardType => "SPC";
    public int NodeId { get; set; }
    public string Dof { get; set; } = string.Empty;
    public double Value { get; set; }
}
