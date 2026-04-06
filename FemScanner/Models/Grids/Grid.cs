namespace FemScanner.Models.Grids;

/// <summary>GRID 카드 모델</summary>
public class Grid
{
    public int Id { get; set; }
    public int CoordId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public int OutCoordId { get; set; }
}
