using FemScanner.Models;

namespace FemScanner.Models.BoundaryConditions;

/// <summary>MPC 다중점 구속 카드</summary>
public class Mpc : IBoundaryCondition
{
    public int Id { get; set; }
    public string CardType => "MPC";
    public List<MpcTerm> Terms { get; } = [];
}

/// <summary>MPC 구속 항 (노드ID, 자유도, 계수)</summary>
public class MpcTerm
{
    public int NodeId { get; set; }
    public int Dof { get; set; }
    public double Coefficient { get; set; }
}
