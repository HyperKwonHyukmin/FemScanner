using FemScanner.Models;

namespace FemScanner.Models.Materials;

/// <summary>MAT8 직교이방성 쉘 재료 카드</summary>
public class Mat8 : IMaterial
{
    public int Id { get; set; }
    public string CardType => "MAT8";
    public double E1 { get; set; }
    public double E2 { get; set; }
    public double Nu12 { get; set; }
    public double G12 { get; set; }
    public double G1z { get; set; }
    public double G2z { get; set; }
    public double Rho { get; set; }
}
