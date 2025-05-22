using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Utils
{
    public static class ArrayExtensions
    {
        public static T[] GetRow<T>(this T[,] array, int rowIndex)
        {
            int cols = array.GetLength(1);
            T[] row = new T[cols];
            for (int j = 0; j < cols; j++)
            {
                row[j] = array[rowIndex, j];
            }
            return row;
        }
    }
}
