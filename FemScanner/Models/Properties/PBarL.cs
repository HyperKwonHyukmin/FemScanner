using FemScanner.Models;

namespace FemScanner.Models.Properties;

/// <summary>PBARL 보 단면 라이브러리 속성 카드 (단면 치수 기반)</summary>
public class PBarL : IProperty
{
    public int Id { get; set; }
    public string CardType => "PBARL";
    public int MaterialId { get; set; }
    /// <summary>단면 형상 그룹 (기본: MSCBML0)</summary>
    public string Group { get; set; } = "MSCBML0";
    /// <summary>단면 형상 타입 (ROD, TUBE, I, L, T, BOX, BAR 등)</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>단면 치수값 배열 (TYPE에 따라 개수 상이)</summary>
    public double[] Dimensions { get; set; } = [];
}
