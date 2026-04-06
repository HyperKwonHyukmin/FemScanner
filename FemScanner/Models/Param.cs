namespace FemScanner.Models;

/// <summary>PARAM 카드 (해석 파라미터 이름-값 쌍)</summary>
public class Param
{
    public string Name { get; set; } = string.Empty;
    public string V1 { get; set; } = string.Empty;
    public string V2 { get; set; } = string.Empty;
}
