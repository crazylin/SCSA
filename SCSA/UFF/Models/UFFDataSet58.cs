using System.Text;

namespace SCSA.UFF.Models;

public class UFFDataSet58
{
    // ID Lines
    public string Id1 { get; set; } = "NONE";
    public string Id2 { get; set; } = "NONE";
    public string Id3 { get; set; } = "NONE";
    public string Id4 { get; set; } = "NONE";
    public string Id5 { get; set; } = "NONE";

    // DOF Identification
    public int FuncType { get; set; } = 0; // 0 - General or Unknown
    public int FuncId { get; set; } = 0;
    public int VerNum { get; set; } = 0;
    public int LoadCaseId { get; set; } = 0;
    public string RspEntName { get; set; } = "NONE";
    public int RspNode { get; set; } = 1;
    public int RspDir { get; set; } = 1;
    public string RefEntName { get; set; } = "NONE";
    public int RefNode { get; set; } = 1;
    public int RefDir { get; set; } = 1;

    // Data Form
    public int OrdDataType { get; set; } = 4; // 4 - real, double precision
    public int NumPts { get; set; }
    public int AbscissaSpacing { get; set; } = 1; // 1 - even spacing
    public double AbscissaMin { get; set; }
    public double AbscissaInc { get; set; }
    public double ZAxisValue { get; set; } = 0.0;

    // Abscissa Data Characteristics
    public int AbscissaSpecDataType { get; set; } = 0;
    public int AbscissaLenUnitExp { get; set; } = 0;
    public int AbscissaForceUnitExp { get; set; } = 0;
    public int AbscissaTempUnitExp { get; set; } = 0;
    public string AbscissaAxisLab { get; set; } = "NONE";
    public string AbscissaAxisUnitsLab { get; set; } = "NONE";

    // Ordinate Data Characteristics
    public int OrdinateSpecDataType { get; set; } = 0;
    public int OrdinateLenUnitExp { get; set; } = 0;
    public int OrdinateForceUnitExp { get; set; } = 0;
    public int OrdinateTempUnitExp { get; set; } = 0;
    public string OrdinateAxisLab { get; set; } = "NONE";
    public string OrdinateAxisUnitsLab { get; set; } = "NONE";

    // Ordinate Denominator Data Characteristics
    public int OrdDenomSpecDataType { get; set; } = 0;
    public int OrdDenomLenUnitExp { get; set; } = 0;
    public int OrdDenomForceUnitExp { get; set; } = 0;
    public int OrdDenomTempUnitExp { get; set; } = 0;
    public string OrdDenomAxisLab { get; set; } = "NONE";
    public string OrdDenomAxisUnitsLab { get; set; } = "NONE";

    // Z-Axis Data Characteristics
    public int ZAxisSpecDataType { get; set; } = 0;
    public int ZAxisLenUnitExp { get; set; } = 0;
    public int ZAxisForceUnitExp { get; set; } = 0;
    public int ZAxisTempUnitExp { get; set; } = 0;
    public string ZAxisAxisLab { get; set; } = "NONE";
    public string ZAxisAxisUnitsLab { get; set; } = "NONE";

    // Data
    public double[] Data { get; set; }
    public double[] X { get; set; }
    public bool IsBinary { get; set; } = false;
    public bool IsComplex { get; set; } = false;
    public bool IsIQSignal { get; set; } = false;
    public int NBytes { get; set; }
    public int ByteOrdering { get; set; } = 1; // 1 for little endian, 2 for big endian

    public string GetHeaderString()
    {
        var sb = new StringBuilder();

        // Write dataset header
        sb.AppendLine("    -1");
        sb.AppendLine("    58");

        // Write binary header if needed
        if (IsBinary)
            sb.AppendLine($"b{ByteOrdering,6}{2,6}{11,12}{NBytes,12}{0,6}{0,6}{0,12}{0,12}");
        else
            sb.AppendLine("                                                                          ");

        // ID Lines
        sb.AppendLine(Id1.PadRight(80));
        sb.AppendLine(Id2.PadRight(80));
        sb.AppendLine(Id3.PadRight(80));
        sb.AppendLine(Id4.PadRight(80));
        sb.AppendLine(Id5.PadRight(80));

        // DOF Identification
        sb.AppendLine(
            $"{FuncType,5}{FuncId,10}{VerNum,5}{LoadCaseId,10} {RspEntName.PadRight(10),10}{RspNode,10}{RspDir,4} {RefEntName.PadRight(10),10}{RefNode,10}{RefDir,4}");

        // Data Form
        sb.AppendLine(
            $"{OrdDataType,10}{NumPts,10}{AbscissaSpacing,10}{AbscissaMin,13:E5}{AbscissaInc,13:E5}{ZAxisValue,13:E5}");

        // Abscissa Data Characteristics
        sb.AppendLine(
            $"{AbscissaSpecDataType,10}{AbscissaLenUnitExp,5}{AbscissaForceUnitExp,5}{AbscissaTempUnitExp,5} {AbscissaAxisLab.PadRight(20),20} {AbscissaAxisUnitsLab.PadRight(20),20}");

        // Ordinate Data Characteristics
        sb.AppendLine(
            $"{OrdinateSpecDataType,10}{OrdinateLenUnitExp,5}{OrdinateForceUnitExp,5}{OrdinateTempUnitExp,5} {OrdinateAxisLab.PadRight(20),20} {OrdinateAxisUnitsLab.PadRight(20),20}");

        // Ordinate Denominator Data Characteristics
        sb.AppendLine(
            $"{OrdDenomSpecDataType,10}{OrdDenomLenUnitExp,5}{OrdDenomForceUnitExp,5}{OrdDenomTempUnitExp,5} {OrdDenomAxisLab.PadRight(20),20} {OrdDenomAxisUnitsLab.PadRight(20),20}");

        // Z-Axis Data Characteristics
        sb.AppendLine(
            $"{ZAxisSpecDataType,10}{ZAxisLenUnitExp,5}{ZAxisForceUnitExp,5}{ZAxisTempUnitExp,5} {ZAxisAxisLab.PadRight(20),20} {ZAxisAxisUnitsLab.PadRight(20),20}");

        return sb.ToString();
    }
}