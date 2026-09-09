using System.Data;

namespace Task2
{
    class MultiplyMatrix
    {
        static public int[,] MultiplyMatrix(int[,] firstMatrix, int[,] secondMatrix)
        {
            int[,] newMatrix = new int[,]()
        }
        static public int[,] MultiplyMatrixCooler(int[,] firstMatrix, int[,] secondMatrix, int potoky)
        {

        }
        private static int[,] GenerateMatrix(int rows, int cols)
        {
            int[,] matrix = new int[rows, cols];
            var rnd = new Random();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = rnd.Next(1, 100);
                }
            }

            return matrix;
        }

        public static void DisplayMatrix(int[,] matrix)
        {
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    Console.Write($"{matrix[i, j],4} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

    }
}