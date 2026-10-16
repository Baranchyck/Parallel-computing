using System.Diagnostics;

class Program
{
    const int INF = 1_000_000_000; 

    static int[,] GenerateGraph(int n, int seed)
    {
        var rnd = new Random(seed);
        var w = new int[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (i == j) w[i, j] = 0;
                else w[i, j] = rnd.NextDouble() < 0.1 ? rnd.Next(1, 100) : INF;
            }
        return w;
    }

    static int[,] InitNext(int[,] w)
    {
        int n = w.GetLength(0);
        var next = new int[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                next[i, j] = (i != j && w[i, j] < INF) ? j : -1;
        return next;
    }

    static void RelaxRows(int[,] d, int[,] next, int m, int from, int to)
    {
        int n = d.GetLength(0);
        for (int i = from; i < to; i++)
            for (int j = 0; j < n; j++)
            {
                int newDist = d[i, m] + d[m, j];
                if (newDist < d[i, j])
                {
                    d[i, j] = newDist;
                    next[i, j] = next[i, m];
                }
            }
    }

    static (int[,] d, int[,] next) FloydSequential(int[,] w)
    {
        int n = w.GetLength(0);
        var d = (int[,])w.Clone();
        var next = InitNext(w);

        for (int m = 0; m < n; m++)
            RelaxRows(d, next, m, 0, n);

        return (d, next);
    }

    static (int[,] d, int[,] next) FloydThreads(int[,] w, int k)
    {
        int n = w.GetLength(0);
        var d = (int[,])w.Clone();
        var next = InitNext(w);
        int size = (n + k - 1) / k; 

        var barrier = new Barrier(k);
        var threads = new Thread[k];

        for (int t = 0; t < k; t++)
        {
            int from = Math.Min(n, t * size);
            int to = Math.Min(n, from + size);

            threads[t] = new Thread(() =>
            {
                for (int m = 0; m < n; m++)
                {
                    RelaxRows(d, next, m, from, to);
                    barrier.SignalAndWait();
                }
            });
            threads[t].Start();
        }

        foreach (var th in threads)
            th.Join();

        barrier.Dispose();

        return (d, next);
    }

    static (int[,] d, int[,] next) FloydTasks(int[,] w, int k)
    {
        int n = w.GetLength(0);
        var d = (int[,])w.Clone();
        var next = InitNext(w);
        int size = (n + k - 1) / k;

        for (int m = 0; m < n; m++)
        {
            int mm = m;
            var tasks = new Task[k];

            for (int t = 0; t < k; t++)
            {
                int from = Math.Min(n, t * size);
                int to = Math.Min(n, from + size);
                tasks[t] = Task.Run(() => RelaxRows(d, next, mm, from, to));
            }

            Task.WaitAll(tasks);
        }

        return (d, next);
    }

    static string PathToString(int[,] d, int[,] next, int a, int b)
    {
        if (d[a, b] >= INF)
            return $"a{a} -> a{b}: шляху немає";

        var steps = new List<int> { a };
        int cur = a;
        while (cur != b)
        {
            cur = next[cur, b];
            steps.Add(cur);
        }

        string path = steps.Count <= 12
            ? string.Join(" -> ", steps.Select(v => $"a{v}"))
            : $"{steps.Count} вершин (a{a} -> ... -> a{b})";

        return $"a{a} -> a{b}, довжина {d[a, b]}, шлях: {path}";
    }

    static long MeasureMs(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        List<int> n_list = [100, 200, 500, 800, 1000, 2000];
        List<int> k_list = [2, 4, 8, 16, 32, 64];

        var warm = GenerateGraph(50, 1);
        FloydSequential(warm);
        FloydThreads(warm, 2);
        FloydTasks(warm, 2);

        Console.WriteLine($"Логічних ядер: {Environment.ProcessorCount}\n");
        Console.WriteLine("    n |   k | Послідовно, мс | Thread, мс | Task, мс | Приск. Thread | Приск. Task");
        Console.WriteLine(new string('-', 92));

        var paths = new List<string>();

        foreach (int n in n_list)
        {
            var w = GenerateGraph(n, 42);
            int a = 0, b = n - 1;

            (int[,] d, int[,] next) res = default;
            long tSeq = MeasureMs(() => res = FloydSequential(w));

            foreach (int k in k_list)
            {
                long tThread = MeasureMs(() => FloydThreads(w, k));
                long tTask = MeasureMs(() => FloydTasks(w, k));

                double spThread = (double)tSeq / Math.Max(1, tThread);
                double spTask = (double)tSeq / Math.Max(1, tTask);

                Console.WriteLine($"{n,5} | {k,3} | {tSeq,14} | {tThread,10} | {tTask,8} | {spThread,13:F2} | {spTask,11:F2}");
            }

            Console.WriteLine(new string('-', 92));

            paths.Add($"n = {n}: " + PathToString(res.d, res.next, a, b));
        }

        Console.WriteLine("\nШляхи між заданими вузлами:");
        foreach (var p in paths)
            Console.WriteLine(p);
    }
}