using System.Text.RegularExpressions;
using FemScanner.Models;
using FemScanner.Models.BoundaryConditions;
using FemScanner.Models.Elements;
using FemScanner.Models.Grids;
using FemScanner.Models.Loads;
using FemScanner.Models.Materials;
using FemScanner.Models.Properties;

namespace FemScanner.Parsers;

/// <summary>
/// Parses a BDF file into a <see cref="BdfModel"/>.
/// </summary>
public class BdfParser
{
    private readonly Dictionary<string, Action<string[], BdfModel, int>> _parsers;

    public BdfParser()
    {
        _parsers = new Dictionary<string, Action<string[], BdfModel, int>>(StringComparer.OrdinalIgnoreCase)
        {
            ["GRID"] = ParseGrid,
            ["CQUAD4"] = (t, m, l) => ParseElement(t, m, l, 4, ids => new CQuad4 { Id = ids[0], PropertyId = ids[1], NodeIds = ids[2..] }),
            ["CTRIA3"] = (t, m, l) => ParseElement(t, m, l, 3, ids => new CTria3 { Id = ids[0], PropertyId = ids[1], NodeIds = ids[2..] }),
            ["CTETRA"] = (t, m, l) => ParseElement(t, m, l, 4, ids => new CTetra { Id = ids[0], PropertyId = ids[1], NodeIds = ids[2..] }),
            ["CHEXA"] = (t, m, l) => ParseElement(t, m, l, 8, ids => new CHexa { Id = ids[0], PropertyId = ids[1], NodeIds = ids[2..] }),
            ["CROD"] = (t, m, l) => ParseElement(t, m, l, 2, ids => new CRod { Id = ids[0], PropertyId = ids[1], NodeIds = ids[2..] }),
            ["CBAR"] = ParseCBar,
            ["CBEAM"] = ParseCBeam,
            ["RBE2"] = ParseRbe2,
            ["CONM2"] = ParseConM2,
            ["PSHELL"] = (t, m, l) => m.Properties.Add(new PShell { Id = ParseInt(t, 1, 0), MaterialId = ParseInt(t, 2, 0), Thickness = ParseDouble(t, 3, 0) }),
            ["PSOLID"] = (t, m, l) => m.Properties.Add(new PSolid { Id = ParseInt(t, 1, 0), MaterialId = ParseInt(t, 2, 0) }),
            ["PBAR"] = (t, m, l) => m.Properties.Add(new PBar { Id = ParseInt(t, 1, 0), MaterialId = ParseInt(t, 2, 0), A = ParseDouble(t, 3, 0), I1 = ParseDouble(t, 4, 0), I2 = ParseDouble(t, 5, 0), J = ParseDouble(t, 6, 0) }),
            ["PBARL"] = ParsePBarL,
            ["PBEAM"] = (t, m, l) => m.Properties.Add(new PBeam { Id = ParseInt(t, 1, 0), MaterialId = ParseInt(t, 2, 0), A = ParseDouble(t, 3, 0), I1 = ParseDouble(t, 4, 0), I2 = ParseDouble(t, 5, 0), J = ParseDouble(t, 6, 0) }),
            ["PBEAML"] = ParsePBeamL,
            ["PROD"] = (t, m, l) => m.Properties.Add(new PRod { Id = ParseInt(t, 1, 0), MaterialId = ParseInt(t, 2, 0), A = ParseDouble(t, 3, 0), J = ParseDouble(t, 4, 0) }),
            ["MAT1"] = (t, m, l) => m.Materials.Add(new Mat1 { Id = ParseInt(t, 1, 0), E = ParseDouble(t, 2, 0), G = ParseDouble(t, 3, 0), Nu = ParseDouble(t, 4, 0), Rho = ParseDouble(t, 5, 0) }),
            ["MAT2"] = (t, m, l) => m.Materials.Add(new Mat2 { Id = ParseInt(t, 1, 0), G11 = ParseDouble(t, 2, 0), G12 = ParseDouble(t, 3, 0), G13 = ParseDouble(t, 4, 0), G22 = ParseDouble(t, 5, 0), G23 = ParseDouble(t, 6, 0), G33 = ParseDouble(t, 7, 0), Rho = ParseDouble(t, 8, 0) }),
            ["MAT8"] = (t, m, l) => m.Materials.Add(new Mat8 { Id = ParseInt(t, 1, 0), E1 = ParseDouble(t, 2, 0), E2 = ParseDouble(t, 3, 0), Nu12 = ParseDouble(t, 4, 0), G12 = ParseDouble(t, 5, 0), G1z = ParseDouble(t, 6, 0), G2z = ParseDouble(t, 7, 0), Rho = ParseDouble(t, 8, 0) }),
            ["FORCE"] = ParseForce,
            ["MOMENT"] = ParseMoment,
            ["GRAV"] = ParseGrav,
            ["PLOAD"] = (t, m, l) => m.Loads.Add(new PLoad { Id = ParseInt(t, 1, 0), SubcaseId = ParseInt(t, 1, 0), Pressure = ParseDouble(t, 2, 0), ElementIds = t.Skip(3).Select(s => int.TryParse(s, out int v) ? v : 0).ToArray() }),
            ["PLOAD4"] = (t, m, l) => m.Loads.Add(new PLoad4 { Id = ParseInt(t, 1, 0), SubcaseId = ParseInt(t, 1, 0), ElementId = ParseInt(t, 2, 0), Pressure = ParseDouble(t, 3, 0) }),
            ["PARAM"] = (t, m, l) => m.Params.Add(new Param
            {
                Name = t.Length > 1 ? t[1] : string.Empty,
                V1 = t.Length > 2 ? t[2] : string.Empty,
                V2 = t.Length > 3 ? t[3] : string.Empty,
            }),
            ["SPC"] = (t, m, l) => m.BoundaryConditions.Add(new Spc { Id = ParseInt(t, 1, 0), NodeId = ParseInt(t, 2, 0), Dof = t.Length > 3 ? t[3] : "0", Value = ParseDouble(t, 4, 0) }),
            ["SPC1"] = ParseSpc1,
            ["MPC"] = ParseMpc,
        };
    }

    public BdfModel Parse(string[] lines)
    {
        var model = new BdfModel();
        var caseControlLines = new List<string>();
        var bulkLines = new List<(int LineNumber, string Line)>();
        bool inBulk = false;
        int lineNumber = 0;

        foreach (var line in lines)
        {
            lineNumber++;

            if (line.TrimStart().StartsWith("BEGIN BULK", StringComparison.OrdinalIgnoreCase))
            {
                inBulk = true;
                continue;
            }

            if (line.TrimStart().StartsWith("ENDDATA", StringComparison.OrdinalIgnoreCase))
                break;

            if (!inBulk)
            {
                if (IsBulkDataLine(line))
                {
                    inBulk = true;
                }
                else
                {
                    caseControlLines.Add(line);
                    continue;
                }
            }

            bulkLines.Add((lineNumber, line));
        }

        foreach (var card in CardReader.ReadCards(bulkLines))
        {
            var tokens = card.Tokens;
            string cardName = tokens[0].TrimEnd('*');

            if (_parsers.TryGetValue(cardName, out var parser))
            {
                parser(tokens, model, card.LineNumber);
            }
            else
            {
                model.Warnings.Add($"Line {card.LineNumber}: Unsupported card '{tokens[0]}'");
            }
        }

        if (caseControlLines.Count > 0)
            model.CaseControl = CaseControlParser.Parse(caseControlLines);

        return model;
    }

    private bool IsBulkDataLine(string line)
    {
        var tokens = CardReader.ReadTokens(line);
        if (tokens.Length == 0)
            return false;

        string cardName = tokens[0].TrimEnd('*');
        return _parsers.ContainsKey(cardName);
    }

    protected virtual void ParseGrid(string[] tokens, BdfModel model, int lineNumber)
    {
        if (tokens.Length < 2)
        {
            model.Warnings.Add($"Line {lineNumber}: GRID card has insufficient fields.");
            return;
        }

        var grid = new Grid
        {
            Id = ParseInt(tokens, 1, 0),
            CoordId = ParseInt(tokens, 2, 0),
            X = ParseDouble(tokens, 3, 0.0),
            Y = ParseDouble(tokens, 4, 0.0),
            Z = ParseDouble(tokens, 5, 0.0),
            OutCoordId = ParseInt(tokens, 6, 0),
        };

        model.Grids.Add(grid);
    }

    protected static int ParseInt(string[] tokens, int index, int defaultValue)
    {
        if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
            return defaultValue;

        return int.TryParse(tokens[index], out int value) ? value : defaultValue;
    }

    private static readonly Regex NastranExponentRegex =
        new(@"(\d)([+-]\d)", RegexOptions.Compiled);

    protected static double ParseDouble(string[] tokens, int index, double defaultValue)
    {
        if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
            return defaultValue;

        string value = tokens[index];
        if (!value.Contains('E') && !value.Contains('e'))
            value = NastranExponentRegex.Replace(value, "$1E$2");

        return double.TryParse(
            value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out double parsed)
            ? parsed
            : defaultValue;
    }

    private static void ParseElement(string[] tokens, BdfModel model, int lineNumber, int nodeCount, Func<int[], IElement> factory)
    {
        int needed = 2 + nodeCount;
        if (tokens.Length < needed + 1)
        {
            model.Warnings.Add($"Line {lineNumber}: {tokens[0]} card has insufficient fields.");
            return;
        }

        var ids = new int[2 + nodeCount];
        ids[0] = ParseInt(tokens, 1, 0);
        ids[1] = ParseInt(tokens, 2, 0);

        for (int i = 0; i < nodeCount; i++)
            ids[2 + i] = ParseInt(tokens, 3 + i, 0);

        model.Elements.Add(factory(ids));
    }

    private static void ParseForce(string[] tokens, BdfModel model, int lineNumber)
    {
        model.Loads.Add(new Force
        {
            Id = ParseInt(tokens, 1, 0),
            SubcaseId = ParseInt(tokens, 2, 0),
            NodeId = ParseInt(tokens, 3, 0),
            CoordId = ParseInt(tokens, 4, 0),
            Magnitude = ParseDouble(tokens, 5, 0),
            Direction = [ParseDouble(tokens, 6, 0), ParseDouble(tokens, 7, 0), ParseDouble(tokens, 8, 0)],
        });
    }

    private static void ParseMoment(string[] tokens, BdfModel model, int lineNumber)
    {
        model.Loads.Add(new Moment
        {
            Id = ParseInt(tokens, 1, 0),
            SubcaseId = ParseInt(tokens, 2, 0),
            NodeId = ParseInt(tokens, 3, 0),
            CoordId = ParseInt(tokens, 4, 0),
            Magnitude = ParseDouble(tokens, 5, 0),
            Direction = [ParseDouble(tokens, 6, 0), ParseDouble(tokens, 7, 0), ParseDouble(tokens, 8, 0)],
        });
    }

    private static void ParseSpc1(string[] tokens, BdfModel model, int lineNumber)
    {
        model.BoundaryConditions.Add(new Spc1
        {
            Id = ParseInt(tokens, 1, 0),
            Dof = tokens.Length > 2 ? tokens[2] : "0",
            NodeIds = tokens.Skip(3).Select(s => int.TryParse(s, out int value) ? value : 0).Where(value => value > 0).ToArray(),
        });
    }

    private static void ParseMpc(string[] tokens, BdfModel model, int lineNumber)
    {
        var mpc = new Mpc { Id = ParseInt(tokens, 1, 0) };
        int index = 2;

        while (index + 2 < tokens.Length)
        {
            mpc.Terms.Add(new MpcTerm
            {
                NodeId = ParseInt(tokens, index, 0),
                Dof = ParseInt(tokens, index + 1, 0),
                Coefficient = ParseDouble(tokens, index + 2, 0),
            });
            index += 3;
        }

        model.BoundaryConditions.Add(mpc);
    }

    private static void ParseRbe2(string[] tokens, BdfModel model, int lineNumber)
    {
        int[] dependentIds = tokens.Skip(4)
            .Select(s => int.TryParse(s, out int value) ? value : 0)
            .Where(value => value > 0)
            .ToArray();

        model.Elements.Add(new Rbe2
        {
            Id = ParseInt(tokens, 1, 0),
            IndependentNodeId = ParseInt(tokens, 2, 0),
            ConstraintDofs = tokens.Length > 3 ? tokens[3] : string.Empty,
            DependentNodeIds = dependentIds,
        });
    }

    private static void ParseConM2(string[] tokens, BdfModel model, int lineNumber)
    {
        model.Elements.Add(new ConM2
        {
            Id = ParseInt(tokens, 1, 0),
            NodeId = ParseInt(tokens, 2, 0),
            CoordId = ParseInt(tokens, 3, 0),
            Mass = ParseDouble(tokens, 4, 0),
            Offset = [ParseDouble(tokens, 5, 0), ParseDouble(tokens, 6, 0), ParseDouble(tokens, 7, 0)],
        });
    }

    private static void ParseGrav(string[] tokens, BdfModel model, int lineNumber)
    {
        model.Loads.Add(new Grav
        {
            Id = ParseInt(tokens, 1, 0),
            SubcaseId = 0,
            CoordId = ParseInt(tokens, 2, 0),
            Scale = ParseDouble(tokens, 3, 0),
            Direction = [ParseDouble(tokens, 4, 0), ParseDouble(tokens, 5, 0), ParseDouble(tokens, 6, 0)],
        });
    }

    private static void ParsePBeamL(string[] tokens, BdfModel model, int lineNumber)
    {
        string group = tokens.Length > 3 && !string.IsNullOrWhiteSpace(tokens[3])
            ? tokens[3]
            : "MSCBML0";
        string type = tokens.Length > 4 ? tokens[4] : string.Empty;

        double[] dimensions = tokens.Skip(5)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Select(token => ParseDouble([token], 0, double.NaN))
            .Where(value => !double.IsNaN(value))
            .ToArray();

        model.Properties.Add(new PBeamL
        {
            Id = ParseInt(tokens, 1, 0),
            MaterialId = ParseInt(tokens, 2, 0),
            Group = group,
            Type = type,
            Dimensions = dimensions,
        });
    }

    private static void ParsePBarL(string[] tokens, BdfModel model, int lineNumber)
    {
        string group = tokens.Length > 3 && !string.IsNullOrWhiteSpace(tokens[3])
            ? tokens[3]
            : "MSCBML0";
        string type = tokens.Length > 4 ? tokens[4] : string.Empty;

        double[] dimensions = tokens.Skip(5)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Select(token => ParseDouble([token], 0, double.NaN))
            .Where(value => !double.IsNaN(value))
            .ToArray();

        model.Properties.Add(new PBarL
        {
            Id = ParseInt(tokens, 1, 0),
            MaterialId = ParseInt(tokens, 2, 0),
            Group = group,
            Type = type,
            Dimensions = dimensions,
        });
    }

    private void ParseCBar(string[] tokens, BdfModel model, int lineNumber)
    {
        int g0Raw = 0;
        bool isG0Mode = tokens.Length > 5
                        && !string.IsNullOrWhiteSpace(tokens[5])
                        && int.TryParse(tokens[5], out g0Raw)
                        && g0Raw != 0;

        var element = new CBar
        {
            Id = ParseInt(tokens, 1, 0),
            PropertyId = ParseInt(tokens, 2, 0),
            NodeIds = [ParseInt(tokens, 3, 0), ParseInt(tokens, 4, 0)],
            G0 = isG0Mode ? g0Raw : 0,
            X = isG0Mode
                ? [0.0, 0.0, 0.0]
                : [ParseDouble(tokens, 5, 0), ParseDouble(tokens, 6, 0), ParseDouble(tokens, 7, 0)],
        };

        model.Elements.Add(element);
    }

    private void ParseCBeam(string[] tokens, BdfModel model, int lineNumber)
    {
        int g0Raw = 0;
        bool isG0Mode = tokens.Length > 5
                        && !string.IsNullOrWhiteSpace(tokens[5])
                        && int.TryParse(tokens[5], out g0Raw)
                        && g0Raw != 0;

        var element = new CBeam
        {
            Id = ParseInt(tokens, 1, 0),
            PropertyId = ParseInt(tokens, 2, 0),
            NodeIds = [ParseInt(tokens, 3, 0), ParseInt(tokens, 4, 0)],
            G0 = isG0Mode ? g0Raw : 0,
            X = isG0Mode
                ? [0.0, 0.0, 0.0]
                : [ParseDouble(tokens, 5, 0), ParseDouble(tokens, 6, 0), ParseDouble(tokens, 7, 0)],
        };

        model.Elements.Add(element);
    }

    protected Dictionary<string, Action<string[], BdfModel, int>> Parsers => _parsers;
}
