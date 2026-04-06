using FemScanner.Models.Grids;

namespace FemScanner.Models;

/// <summary>파싱된 BDF 파일 전체 데이터 컨테이너</summary>
public class BdfModel
{
    public List<Grid> Grids { get; } = [];
    public List<IElement> Elements { get; } = [];
    public List<IProperty> Properties { get; } = [];
    public List<IMaterial> Materials { get; } = [];
    public List<ILoad> Loads { get; } = [];
    public List<IBoundaryCondition> BoundaryConditions { get; } = [];
    public CaseControl CaseControl { get; set; } = new();
    public List<Param> Params { get; } = [];
    public List<string> Warnings { get; } = [];
}
