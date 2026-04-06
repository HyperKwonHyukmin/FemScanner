using FemScanner.Models;

namespace FemScanner.Models.Properties;

/// <summary>PSHELL 쉘 속성 카드</summary>
public class PShell : IProperty
{
    public int Id { get; set; }
    public string CardType => "PSHELL";
    public int MaterialId { get; set; }
    public double Thickness { get; set; }
    public int MaterialId2 { get; set; }
    public double Bending { get; set; }
    public int MaterialId3 { get; set; }
    public double TransShear { get; set; }
}
