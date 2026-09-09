using System.Diagnostics;

namespace Task2
{
    public class MatrixMultiplying
    {
        public static int[,] MultiplyMatrix(int[,] firstMatrix, int[,] secondMatrix)
        {
            int n = firstMatrix.GetLength(0);
            int[,] newMatrix = new int[n, n];

            Stopwatch stopWatch = Stopwatch.StartNew();

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < n; k++)
                {
                    int r = firstMatrix[i, k];
                    for (int j = 0; j < n; j++)
                    {
                        newMatrix[i, j] += r * secondMatrix[k, j];
                    }
                }
            }

            stopWatch.Stop();
            Console.WriteLine($"Avarage time: {stopWatch.Elapsed}");

            return newMatrix;
        }

        private static Thread StartWorker(int[,] firstMatrix, int[,] secondMatrix, int[,] newMatrix, int startRow, int endRow, int n)
        {
            Thread thread = new Thread(() =>
            {
                for (int i = startRow; i < endRow; i++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        int r = firstMatrix[i, k];
                        for (int j = 0; j < n; j++)
                        {
                            newMatrix[i, j] += r * secondMatrix[k, j];
                        }
                    }
                }
            });

            thread.Start();
            return thread;
        }

        public static int[,] MultiplyMatrixCooler(int[,] firstMatrix, int[,] secondMatrix, int potoky)
        {
            int n = firstMatrix.GetLength(0);
            int[,] newMatrix = new int[n, n];

            Thread[] threads = new Thread[potoky];
            int rowsPerThread = n / potoky;

            Stopwatch stopWatch = Stopwatch.StartNew();

            for (int i = 0; i < potoky; i++)
            {
                int startRow = i * rowsPerThread;
                int endRow = (i == potoky - 1) ? n : (i + 1) * rowsPerThread;

                threads[i] = StartWorker(firstMatrix, secondMatrix, newMatrix, startRow, endRow, n);
            }

            for (int i = 0; i < potoky; i++)
            {
                threads[i].Join();
            }

            stopWatch.Stop();
            Console.WriteLine($"Parallel time: {stopWatch.Elapsed} with {potoky} threads");

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
                    matrix[i, j] = rnd.Next(1, 10); 
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

            //int potoky = Environment.ProcessorCount;
            int potoky = 5;

            Console.WriteLine($"potoky = {potoky}");

            Console.WriteLine("Прогрів: ");
            int warmupSize = 100;
            int[,] w1 = GenerateMatrix(warmupSize, warmupSize);
            int[,] w2 = GenerateMatrix(warmupSize, warmupSize);

            MultiplyMatrix(w1, w2);
            MultiplyMatrixCooler(w1, w2, potoky);

            Console.WriteLine("----------------------------------");

            List<int> n = [200, 400, 800, 1600, 2400];

            for (int i = 0; i < n.Count; i++)
            {
                int size = n[i];
                Console.WriteLine($"Розмір: {size} x {size}");

                int[,] m1 = GenerateMatrix(size, size);
                int[,] m2 = GenerateMatrix(size, size);

                var resSync = MultiplyMatrix(m1, m2);
                var resAsync = MultiplyMatrixCooler(m1, m2, potoky);

                Console.WriteLine("----------------------------");
            }

            Console.WriteLine("Тестова матриця 10x10:");
            var m = GenerateMatrix(10, 10);
            DisplayMatrix(m);
        }
    }
}