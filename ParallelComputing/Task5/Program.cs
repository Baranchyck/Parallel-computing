using System.Diagnostics;

class Program
{
    const int INF = 1_000_000_000; // "ребра немає"

    // ---------- Генерація графа ----------
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

    // next[i,j] - яка вершина йде першою на шляху з i в j (для відновлення шляху)
    static int[,] InitNext(int[,] w)
    {
        int n = w.GetLength(0);
        var next = new int[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                next[i, j] = (i != j && w[i, j] < INF) ? j : -1;
        return next;
    }

    // ---------- Крок Флойда для рядків [from, to) при проміжній вершині m ----------
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

    // ---------- 1. Послідовний ----------
    static (int[,] d, int[,] next) FloydSequential(int[,] w)
    {
        int n = w.GetLength(0);
        var d = (int[,])w.Clone();
        var next = InitNext(w);

        for (int m = 0; m < n; m++)
            RelaxRows(d, next, m, 0, n);

        return (d, next);
    }

    // ---------- 2. Thread ----------
    static (int[,] d, int[,] next) FloydThreads(int[,] w, int k)
    {
        int n = w.GetLength(0);
        var d = (int[,])w.Clone();
        var next = InitNext(w);
        int size = (n + k - 1) / k; // рядків на один потік

        for (int m = 0; m < n; m++)
        {
            int mm = m;
            var threads = new Thread[k];

            for (int t = 0; t < k; t++)
            {
                int from = t * size;
                int to = Math.Min(n, from + size);
                threads[t] = new Thread(() => RelaxRows(d, next, mm, from, to));
                threads[t].Start();
            }

            foreach (var th in threads)
                th.Join();
        }

        return (d, next);
    }

    // ---------- 3. Task ----------
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
                int from = t * size;
                int to = Math.Min(n, from + size);
                tasks[t] = Task.Run(() => RelaxRows(d, next, mm, from, to));
            }

            Task.WaitAll(tasks);
        }

        return (d, next);
    }

    // ---------- Вивід шляху a -> b ----------
    static void PrintPath(int[,] d, int[,] next, int a, int b)
    {
        if (d[a, b] >= INF)
        {
            Console.WriteLine($"Шляху a{a} -> a{b} немає");
            return;
        }

        Console.Write($"Довжина a{a} -> a{b} = {d[a, b]}, шлях: a{a}");
        int cur = a;
        while (cur != b)
        {
            cur = next[cur, b];
            Console.Write($" -> a{cur}");
        }
        Console.WriteLine();
    }

    // ---------- Заміри часу ----------
    static long MeasureMs(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    static void Main()
    {
        int n = 800;        // кількість вершин
        int k = 4;          // кількість потоків
        int a = 0, b = 700; // початковий і кінцевий вузли

        var w = GenerateGraph(n, 42);

        (int[,] d, int[,] next) res = default;

        long tSeq = MeasureMs(() => res = FloydSequential(w));
        Console.WriteLine($"Послідовно:  {tSeq} мс");

        long tThread = MeasureMs(() => FloydThreads(w, k));
        Console.WriteLine($"Thread ({k}):  {tThread} мс");

        long tTask = MeasureMs(() => FloydTasks(w, k));
        Console.WriteLine($"Task ({k}):    {tTask} мс");

        PrintPath(res.d, res.next, a, b);
    }
}