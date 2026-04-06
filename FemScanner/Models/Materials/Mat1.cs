using FemScanner.Models;

namespace FemScanner.Models.Materials;

/// <summary>MAT1 등방성 재료 카드</summary>
public class Mat1 : IMaterial
{
    public int Id { get; set; }
    public string CardType => "MAT1";
    public double E { get; set; }
    public double G { get; set; }
    public double Nu { get; set; }
    public double Rho { get; set; }
    public double A { get; set; }
    public double TRef { get; set; }
}
