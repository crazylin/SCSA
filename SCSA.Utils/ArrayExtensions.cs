namespace SCSA.Utils;

public static class ArrayExtensions
{
    public static T[] GetRow<T>(this T[,] array, int rowIndex)
    {
        var cols = array.GetLength(1);
        T[] row = new T[cols];
        for (var j = 0; j < cols; j++) row[j] = array[rowIndex, j];
        return row;
    }
}