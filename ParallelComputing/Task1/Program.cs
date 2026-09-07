using System.Diagnostics;

namespace Task1
{
    public class MatrixAdding
    {
        public static int[,] AddMatrix(int[,] firstMatrix, int[,] secondMatrix)
        {
            int rows = firstMatrix.GetLength(0);
            int cols = firstMatrix.GetLength(1);

            int[,] newMatrix = new int[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newMatrix[i, j] = firstMatrix[i, j] + secondMatrix[i, j];
                }
            }

            return newMatrix;
        }
        //розібратись
        private static Thread StartWorker(int[,] a, int[,] b, int[,] res, int start, int end, int cols)
        {
            Thread thread = new Thread(() =>
            {
                for (int i = start; i < end; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        res[i, j] = a[i, j] + b[i, j];
                    }
                }
            });

            thread.Start();
            return thread;
        }
        public static int[,] AddMatrixCooler(int[,] firstMatrix, int[,] secondMatrix, int potoky)
        {
            int rows = firstMatrix.GetLength(0);
            int cols = firstMatrix.GetLength(1);
            int[,] newMatrix = new int[rows, cols];

            Thread[] threads = new Thread[potoky];
            int rowsPerThread = rows / potoky;

            Stopwatch stopWatch = Stopwatch.StartNew();

            for (int i = 0; i < potoky; i++)
            {
                int startRow = i * rowsPerThread;
                int endRow = (i == potoky - 1) ? rows : (i + 1) * rowsPerThread;

                threads[i] = StartWorker(firstMatrix, secondMatrix, newMatrix, startRow, endRow, cols);
            }

            for (int i = 0; i < potoky; i++)
            {
                threads[i].Join();
            }

            stopWatch.Stop();
            Console.WriteLine($"Async time ~ {stopWatch.Elapsed} with {potoky} threads");

            return newMatrix;
        }

//нада фіксіть і розбиратись
        private static int[,] GenerateMatrix(int rows, int cols)
        {
            var matrix = new int[rows, cols];
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

        static void Main()
        {
            int rows = 4000;
            int cols = 4000;
            int potoky = Environment.ProcessorCount; 

            Console.WriteLine($"Генерація матриць {rows}x{cols}...");
            var m1 = GenerateMatrix(rows, cols);
            var m2 = GenerateMatrix(rows, cols);

            Console.WriteLine("\n--- Старт обчислень ---");

            var sw = Stopwatch.StartNew();
            var resSync = AddMatrix(m1, m2);
            sw.Stop();
            Console.WriteLine($"Sync time  ~ {sw.Elapsed}");

            var resAsync = AddMatrixCooler(m1, m2, potoky);

            bool isCorrect = resSync[0, 0] == resAsync[0, 0] &&
                             resSync[rows - 1, cols - 1] == resAsync[rows - 1, cols - 1];
            Console.WriteLine($"\nРезультати зійшлися: {isCorrect}");
        }
    }
}