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

            Stopwatch stopWatch = Stopwatch.StartNew();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newMatrix[i, j] = firstMatrix[i, j] + secondMatrix[i, j];
                }
            }

            stopWatch.Stop();
            Console.WriteLine($"Avarage time ~ {stopWatch.Elapsed}");

            return newMatrix;
        }

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
            Console.WriteLine($"Parallel time ~ {stopWatch.Elapsed} with {potoky} threads");

            return newMatrix;
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

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("Прогрів: ");
            int warmupSize = 100;
            int[,] w1 = GenerateMatrix(warmupSize, warmupSize);
            int[,] w2 = GenerateMatrix(warmupSize, warmupSize);

            AddMatrix(w1, w2);
            AddMatrixCooler(w1, w2, 16);

            Console.WriteLine("----------------------------------");


            List<int> rows = [200, 400, 800, 1600, 3200, 6400, 12800];
            List<int> cols = [200, 400, 800, 1600, 3200, 6400, 12800];

            int potoky = Environment.ProcessorCount;

            for (int i = 0; i < rows.Count; i++)
            {
                int r = rows[i];
                int c = cols[i];

                Console.WriteLine($"Розмір: {r} x {c}");

                int[,] m1 = GenerateMatrix(r, c);
                int[,] m2 = GenerateMatrix(r, c);

                var resSync = AddMatrix(m1, m2);
                var resAsync = AddMatrixCooler(m1, m2, potoky);

                Console.WriteLine("----------------------------");
            }

            Console.WriteLine("Тестова матриця 10x10:");
            var m = GenerateMatrix(10, 10);
            DisplayMatrix(m);
        }
    }
}