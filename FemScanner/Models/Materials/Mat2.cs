using FemScanner.Models;

namespace FemScanner.Models.Materials;

/// <summary>MAT2 비등방성 쉘 재료 카드</summary>
public class Mat2 : IMaterial
{
    public int Id { get; set; }
    public string CardType => "MAT2";
    public double G11 { get; set; }
    public double G12 { get; set; }
    public double G13 { get; set; }
    public double G22 { get; set; }
    public double G23 { get; set; }
    public double G33 { get; set; }
    public double Rho { get; set; }
}
