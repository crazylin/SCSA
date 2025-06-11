namespace SCSA.Utils;

public static class Window
{
    public static double[] GetWindow(WindowFunction windowFunction, int len)
    {
        // add coherent gain - http://www.ni.com/white-paper/4278/en
        var data = new double[len];
        switch (windowFunction)
        {
            case WindowFunction.Hamming:
            {
                var factor = 1.852;
                var tempData = MathNet.Numerics.Window.Hamming(len);
                for (var i = 0; i < data.Length; i++) data[i] = tempData[i] * factor;
            }
                break;
            case WindowFunction.Hann:
            {
                var factor = 2.0;
                var tempData = MathNet.Numerics.Window.Hann(len);
                for (var i = 0; i < data.Length; i++) data[i] = tempData[i] * factor;
            }
                break;
            case WindowFunction.Blackman:
            {
                var factor = 2.3809524;
                var tempData = MathNet.Numerics.Window.Blackman(len);
                for (var i = 0; i < data.Length; i++) data[i] = tempData[i] * factor;
            }
                break;
            case WindowFunction.BlackmanHarris:
            {
                var factor = 2.7874564;
                var tempData = MathNet.Numerics.Window.BlackmanHarris(len);
                for (var i = 0; i < data.Length; i++) data[i] = tempData[i] * factor;
            }
                break;
            case WindowFunction.FlatTop:
                return MathNet.Numerics.Window.FlatTop(len);
            case WindowFunction.Triangular:
            {
                var factor = 2.0;
                var tempData = MathNet.Numerics.Window.Triangular(len);
                for (var i = 0; i < data.Length; i++) data[i] = tempData[i] * factor;
            }
                break;
            case WindowFunction.Rectangle:
            default:
                for (var i = 0; i < data.Length; i++) data[i] = 1;

                break;
        }

        return data;
    }
}

public enum WindowFunction
{
    Rectangle,
    Hamming,
    Hann,
    Blackman,
    BlackmanHarris,
    FlatTop,
    Triangular
}