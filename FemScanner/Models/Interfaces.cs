using System.Text.Json.Serialization;
using FemScanner.Models.BoundaryConditions;
using FemScanner.Models.Elements;
using FemScanner.Models.Loads;
using FemScanner.Models.Materials;
using FemScanner.Models.Properties;

namespace FemScanner.Models;

/// <summary>BDF 요소 카드 공통 인터페이스</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "cardType")]
[JsonDerivedType(typeof(CQuad4), "CQUAD4")]
[JsonDerivedType(typeof(CTria3), "CTRIA3")]
[JsonDerivedType(typeof(CTetra), "CTETRA")]
[JsonDerivedType(typeof(CHexa), "CHEXA")]
[JsonDerivedType(typeof(CBar), "CBAR")]
[JsonDerivedType(typeof(CBeam), "CBEAM")]
[JsonDerivedType(typeof(CRod), "CROD")]
[JsonDerivedType(typeof(Rbe2), "RBE2")]
[JsonDerivedType(typeof(ConM2), "CONM2")]
public interface IElement
{
    int Id { get; }
    int PropertyId { get; }
    int[] NodeIds { get; }
    string CardType { get; }
}

/// <summary>BDF 속성 카드 공통 인터페이스</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "cardType")]
[JsonDerivedType(typeof(PShell), "PSHELL")]
[JsonDerivedType(typeof(PSolid), "PSOLID")]
[JsonDerivedType(typeof(PBar), "PBAR")]
[JsonDerivedType(typeof(PBarL), "PBARL")]
[JsonDerivedType(typeof(PBeam), "PBEAM")]
[JsonDerivedType(typeof(PBeamL), "PBEAML")]
[JsonDerivedType(typeof(PRod), "PROD")]
public interface IProperty
{
    int Id { get; }
    string CardType { get; }
}

/// <summary>BDF 재료 카드 공통 인터페이스</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "cardType")]
[JsonDerivedType(typeof(Mat1), "MAT1")]
[JsonDerivedType(typeof(Mat2), "MAT2")]
[JsonDerivedType(typeof(Mat8), "MAT8")]
public interface IMaterial
{
    int Id { get; }
    string CardType { get; }
}

/// <summary>BDF 하중 카드 공통 인터페이스</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "cardType")]
[JsonDerivedType(typeof(Force), "FORCE")]
[JsonDerivedType(typeof(Moment), "MOMENT")]
[JsonDerivedType(typeof(PLoad), "PLOAD")]
[JsonDerivedType(typeof(PLoad4), "PLOAD4")]
[JsonDerivedType(typeof(Grav), "GRAV")]
public interface ILoad
{
    int Id { get; }
    int SubcaseId { get; }
    string CardType { get; }
}

/// <summary>BDF 경계조건 카드 공통 인터페이스</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "cardType")]
[JsonDerivedType(typeof(Spc), "SPC")]
[JsonDerivedType(typeof(Spc1), "SPC1")]
[JsonDerivedType(typeof(Mpc), "MPC")]
public interface IBoundaryCondition
{
    int Id { get; }
    string CardType { get; }
}
