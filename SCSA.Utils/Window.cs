namespace SCSA.Utils;

public static class Window
{
    public static double[] GetWindow(WindowFunction windowFunction, int len)
    {
        switch (windowFunction)
        {
            case WindowFunction.Hamming:
                return MathNet.Numerics.Window.Hamming(len);
            case WindowFunction.Hanning:
                return MathNet.Numerics.Window.Hann(len);
            case WindowFunction.Blackman:
                return MathNet.Numerics.Window.Blackman(len);
            case WindowFunction.BlackmanHarris:
                return MathNet.Numerics.Window.BlackmanHarris(len);
            case WindowFunction.FlatTop:
                return MathNet.Numerics.Window.FlatTop(len);
            case WindowFunction.Triangular:
                return MathNet.Numerics.Window.Triangular(len);
            case WindowFunction.Rectangle:
            default:
                var data = new double[len];
                for (var i = 0; i < data.Length; i++) data[i] = 1;
                return data;
        }
    }

    public static double GetEnbw(WindowFunction windowFunction)
    {
        // Equivalent Noise Bandwidth (ENBW) values for various window functions.
        // These are standard, well-known values for a normalized window.
        switch (windowFunction)
        {
            case WindowFunction.Rectangle:
                return 1.0;
            case WindowFunction.Hamming:
                return 1.36;
            case WindowFunction.Hanning:
                return 1.5;
            case WindowFunction.Blackman:
                return 1.73;
            case WindowFunction.BlackmanHarris:
                return 2.0;
            case WindowFunction.FlatTop:
                return 3.77;
            case WindowFunction.Triangular:
                return 1.33;
            default:
                return 1.0;
        }
    }
}

public enum WindowFunction
{
    Rectangle,
    Hamming,
    Hanning,
    Blackman,
    BlackmanHarris,
    FlatTop,
    Triangular
}