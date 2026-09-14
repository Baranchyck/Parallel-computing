using System.Diagnostics;

namespace Task2
{
    public class MatrixMultiplying
    {
        public static int[,] MultiplyMatrix(int[,] firstMatrix, int[,] secondMatrix)
        {
            int rows = firstMatrix.GetLength(0);
            int inner = firstMatrix.GetLength(1);
            int cols = secondMatrix.GetLength(1);
            int[,] newMatrix = new int[rows, cols];

            Stopwatch stopWatch = Stopwatch.StartNew();

            for (int i = 0; i < rows; i++)
            {
                for (int k = 0; k < inner; k++)
                {
                    int r = firstMatrix[i, k];
                    for (int j = 0; j < cols; j++)
                    {
                        newMatrix[i, j] += r * secondMatrix[k, j];
                    }
                }
            }

            stopWatch.Stop();
            Console.WriteLine($"Avarage time: {stopWatch.Elapsed}");

            return newMatrix;
        }

        private static Thread StartWorker(int[,] firstMatrix, int[,] secondMatrix, int[,] newMatrix, int startRow, int endRow, int inner, int cols)
        {
            Thread thread = new Thread(() =>
            {
                for (int i = startRow; i < endRow; i++)
                {
                    for (int k = 0; k < inner; k++)
                    {
                        int r = firstMatrix[i, k];
                        for (int j = 0; j < cols; j++)
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
            int rows = firstMatrix.GetLength(0);
            int inner = firstMatrix.GetLength(1);
            int cols = secondMatrix.GetLength(1);
            int[,] newMatrix = new int[rows, cols];

            Thread[] threads = new Thread[potoky];
            int rowsPerThread = rows / potoky;

            Stopwatch stopWatch = Stopwatch.StartNew();

            for (int i = 0; i < potoky; i++)
            {
                int startRow = i * rowsPerThread;
                int endRow = (i == potoky - 1) ? rows : (i + 1) * rowsPerThread;

                threads[i] = StartWorker(firstMatrix, secondMatrix, newMatrix, startRow, endRow, inner, cols);
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

            Console.WriteLine("Введіть кількість потоків:");
            int potoky = int.Parse(Console.ReadLine()!);

            Console.WriteLine("Прогрів: ");
            int warmupSize = 100;
            int[,] w1 = GenerateMatrix(warmupSize, warmupSize);
            int[,] w2 = GenerateMatrix(warmupSize, warmupSize);
            int pot = 4;
            MultiplyMatrix(w1, w2);
            MultiplyMatrixCooler(w1, w2, pot);

            Console.WriteLine("----------------------------------");

            List<int> rowsList = [10, 50, 100, 200, 300, 400, 800, 1600, 2000, 2400];
            List<int> colsList = [10, 50, 150, 200, 500, 400, 1000, 1600, 2000, 3000];

            Console.WriteLine($"potoky = {potoky}");

            for (int i = 0; i < rowsList.Count; i++)
            {
                int rows = rowsList[i];
                int cols = colsList[i];

                Console.WriteLine($"Розмір: {rows} x {cols}");

                int[,] m1 = GenerateMatrix(rows, cols);
                int[,] m2 = GenerateMatrix(cols, rows);

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