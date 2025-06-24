namespace SCSA.Utils;

public static class UnitConverter
{
    private const double G = 9.80665;
    public static double Convert(double value, PhysicalUnit from, PhysicalUnit to)
    {
        if (from == to) return value;

        // 统一先转换到 SI 基础单位再转到目标
        // 位移基准: 米, 速度: 米/秒, 加速度: 米/秒²
        double baseValue = from switch
        {
            PhysicalUnit.Micrometer           => value * 1e-6,
            PhysicalUnit.Millimeter           => value * 1e-3,
            PhysicalUnit.Meter               => value,
            PhysicalUnit.MicrometerPerSecond  => value * 1e-6,
            PhysicalUnit.MillimeterPerSecond  => value * 1e-3,
            PhysicalUnit.MeterPerSecond       => value,
            PhysicalUnit.MicrometerPerSecond2 => value * 1e-6,
            PhysicalUnit.MillimeterPerSecond2 => value * 1e-3,
            PhysicalUnit.MeterPerSecond2      => value,
            PhysicalUnit.G                    => value * G,
            _ => value
        };

        return to switch
        {
            PhysicalUnit.Micrometer           => baseValue / 1e-6,
            PhysicalUnit.Millimeter           => baseValue / 1e-3,
            PhysicalUnit.Meter               => baseValue,
            PhysicalUnit.MicrometerPerSecond  => baseValue / 1e-6,
            PhysicalUnit.MillimeterPerSecond  => baseValue / 1e-3,
            PhysicalUnit.MeterPerSecond       => baseValue,
            PhysicalUnit.MicrometerPerSecond2 => baseValue / 1e-6,
            PhysicalUnit.MillimeterPerSecond2 => baseValue / 1e-3,
            PhysicalUnit.MeterPerSecond2      => baseValue,
            PhysicalUnit.G                    => baseValue / G,
            _ => baseValue
        };
    }

    public static string GetUnitString(PhysicalUnit unit) => unit switch
    {
        PhysicalUnit.Micrometer              => "µm",
        PhysicalUnit.Millimeter              => "mm",
        PhysicalUnit.Meter                   => "m",
        PhysicalUnit.MicrometerPerSecond     => "µm/s",
        PhysicalUnit.MillimeterPerSecond     => "mm/s",
        PhysicalUnit.MeterPerSecond          => "m/s",
        PhysicalUnit.MicrometerPerSecond2    => "µm/s²",
        PhysicalUnit.MillimeterPerSecond2    => "mm/s²",
        PhysicalUnit.MeterPerSecond2         => "m/s²",
        PhysicalUnit.G                       => "g",
        _                                    => string.Empty
    };
}

public enum PhysicalUnit
{
    // 位移
    Micrometer,
    Millimeter,
    Meter,
    // 速度
    MicrometerPerSecond,
    MillimeterPerSecond,
    MeterPerSecond,
    // 加速度
    MicrometerPerSecond2,
    MillimeterPerSecond2,
    MeterPerSecond2,
    G
}