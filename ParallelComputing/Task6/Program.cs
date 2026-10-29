using System.Diagnostics;

class Program
{
    const int INF = int.MaxValue;

    class Graph
    {
        public int N;
        public int[] Start = [];
        public int[] Adj = [];
        public int[] W = [];
        public int M => Adj.Length / 2;
    }

    static Graph GenerateGraph(int n, int avgDegree, int seed)
    {
        var rnd = new Random(seed);
        var edges = new List<(int u, int v, int w)>();
        var seen = new HashSet<long>();

        void AddEdge(int u, int v)
        {
            if (u == v) return;
            long id = (long)Math.Min(u, v) * n + Math.Max(u, v);
            if (seen.Add(id))
                edges.Add((u, v, rnd.Next(1, 100)));
        }

        var perm = Enumerable.Range(0, n).OrderBy(_ => rnd.Next()).ToArray();
        for (int i = 1; i < n; i++)
            AddEdge(perm[i], perm[rnd.Next(i)]);

        long target = (long)n * avgDegree / 2;
        while (edges.Count < target)
            AddEdge(rnd.Next(n), rnd.Next(n));

        var g = new Graph { N = n, Start = new int[n + 1] };
        foreach (var (u, v, _) in edges) { g.Start[u + 1]++; g.Start[v + 1]++; }
        for (int i = 0; i < n; i++) g.Start[i + 1] += g.Start[i];

        g.Adj = new int[g.Start[n]];
        g.W = new int[g.Start[n]];
        var pos = (int[])g.Start.Clone();
        foreach (var (u, v, w) in edges)
        {
            g.Adj[pos[u]] = v; g.W[pos[u]++] = w;
            g.Adj[pos[v]] = u; g.W[pos[v]++] = w;
        }

        return g;
    }

    static (int[] dist, int[] key, int[] prev) Init(int n, int a)
    {
        var dist = new int[n];
        var prev = new int[n];
        Array.Fill(dist, INF);
        Array.Fill(prev, -1);
        dist[a] = 0;
        return (dist, (int[])dist.Clone(), prev);
    }

    static void Relax(Graph g, int[] dist, int[] key, int[] prev, int u, int from, int to)
    {
        for (int e = g.Start[u]; e < g.Start[u + 1]; e++)
        {
            int v = g.Adj[e];
            if (v < from || v >= to) continue;
            int nd = dist[u] + g.W[e];
            if (nd < dist[v])
            {
                dist[v] = nd;
                key[v] = nd;
                prev[v] = u;
            }
        }
    }

    static (int dist, int v) MinInBlock(int[] key, int from, int to)
    {
        int best = INF, bestV = -1;
        for (int v = from; v < to; v++)
            if (key[v] < best)
            {
                best = key[v];
                bestV = v;
            }
        return (best, bestV);
    }

    static int ArgMin((int dist, int v)[] local)
    {
        var best = (dist: INF, v: -1);
        foreach (var x in local)
            if (x.dist < best.dist) best = x;
        return best.v;
    }

    static (int[] dist, int[] prev) DijkstraSequential(Graph g, int a)
    {
        int n = g.N;
        var (dist, key, prev) = Init(n, a);

        for (int iter = 0; iter < n; iter++)
        {
            int u = MinInBlock(key, 0, n).v;
            if (u == -1) break;
            key[u] = INF;
            Relax(g, dist, key, prev, u, 0, n);
        }

        return (dist, prev);
    }

    static (int[] dist, int[] prev) DijkstraThreads(Graph g, int a, int k)
    {
        int n = g.N;
        var (dist, key, prev) = Init(n, a);
        var local = new[] { new (int, int)[k], new (int, int)[k] };
        int size = (n + k - 1) / k;

        var barrier = new Barrier(k);
        var threads = new Thread[k];

        for (int t = 0; t < k; t++)
        {
            int id = t;
            int from = Math.Min(n, id * size);
            int to = Math.Min(n, from + size);

            threads[t] = new Thread(() =>
            {
                int u = -1;
                for (int iter = 0; iter < n; iter++)
                {
                    if (u != -1)
                        Relax(g, dist, key, prev, u, from, to);

                    var buf = local[iter & 1];
                    buf[id] = MinInBlock(key, from, to);
                    barrier.SignalAndWait();

                    u = ArgMin(buf);
                    if (u == -1) break;
                    if (u >= from && u < to) key[u] = INF;
                }
            });
            threads[t].Start();
        }

        foreach (var th in threads)
            th.Join();

        barrier.Dispose();

        return (dist, prev);
    }

    static (int[] dist, int[] prev) DijkstraTasks(Graph g, int a, int k)
    {
        int n = g.N;
        var (dist, key, prev) = Init(n, a);
        var local = new (int, int)[k];
        int size = (n + k - 1) / k;
        var tasks = new Task[k];
        int u = -1;

        for (int iter = 0; iter < n; iter++)
        {
            int cur = u;
            for (int t = 0; t < k; t++)
            {
                int id = t;
                int from = Math.Min(n, id * size);
                int to = Math.Min(n, from + size);
                tasks[t] = Task.Run(() =>
                {
                    if (cur != -1)
                        Relax(g, dist, key, prev, cur, from, to);
                    local[id] = MinInBlock(key, from, to);
                });
            }
            Task.WaitAll(tasks);

            u = ArgMin(local);
            if (u == -1) break;
            key[u] = INF;
        }

        return (dist, prev);
    }

    static int[] DijkstraHeap(Graph g, int a)
    {
        var dist = new int[g.N];
        Array.Fill(dist, INF);
        dist[a] = 0;
        var pq = new PriorityQueue<int, int>();
        pq.Enqueue(a, 0);

        while (pq.TryDequeue(out int u, out int d))
        {
            if (d > dist[u]) continue;
            for (int e = g.Start[u]; e < g.Start[u + 1]; e++)
            {
                int v = g.Adj[e], nd = d + g.W[e];
                if (nd < dist[v])
                {
                    dist[v] = nd;
                    pq.Enqueue(v, nd);
                }
            }
        }
        return dist;
    }

    static string PathToString(int[] dist, int[] prev, int a, int b)
    {
        if (dist[b] == INF)
            return $"a{a} -> a{b}: шляху немає";

        var steps = new List<int>();
        for (int v = b; v != -1; v = prev[v])
            steps.Add(v);
        steps.Reverse();

        string path = steps.Count <= 12
            ? string.Join(" -> ", steps.Select(v => $"a{v}"))
            : $"{steps.Count} вершин (a{a} -> ... -> a{b})";

        return $"a{a} -> a{b}, довжина {dist[b]}, шлях: {path}";
    }

    static (T result, double ms) Measure<T>(Func<T> f, int repeats)
    {
        T result = default!;
        double best = double.MaxValue;
        for (int r = 0; r < repeats; r++)
        {
            var sw = Stopwatch.StartNew();
            result = f();
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
        }
        return (result, best);
    }

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        List<int> n_list = [1000, 5000, 10000, 20000, 50000, 100000];
        List<int> k_list = [2, 4, 8, 16, 32, 64];
        const int avgDegree = 10;

        var warm = GenerateGraph(500, avgDegree, 1);
        DijkstraSequential(warm, 0);
        DijkstraThreads(warm, 0, 2);
        DijkstraTasks(warm, 0, 2);

        var rnd = new Random(2026);

        Console.WriteLine($"Логічних ядер: {Environment.ProcessorCount}");
        Console.WriteLine($"Граф: неорієнтований, зв'язний, ваги 1..99, середній степінь вершини {avgDegree}\n");
        Console.WriteLine("     n |       m |      a |  k | Послідовно, мс | Thread, мс |   Task, мс | Приск. Thread | Приск. Task");
        Console.WriteLine(new string('-', 106));

        var paths = new List<string>();
        bool allCorrect = true;

        foreach (int n in n_list)
        {
            var g = GenerateGraph(n, avgDegree, 42);
            int a = rnd.Next(n);
            int repeats = n <= 10000 ? 3 : 1;

            var (res, tSeq) = Measure(() => DijkstraSequential(g, a), repeats);
            int[] reference = DijkstraHeap(g, a);
            allCorrect &= res.dist.SequenceEqual(reference);

            foreach (int k in k_list)
            {
                var (rt, tThread) = Measure(() => DijkstraThreads(g, a, k), repeats);
                var (rk, tTask) = Measure(() => DijkstraTasks(g, a, k), repeats);
                allCorrect &= rt.dist.SequenceEqual(reference) && rk.dist.SequenceEqual(reference);

                Console.WriteLine($"{n,6} | {g.M,7} | {"a" + a,6} | {k,2} | {tSeq,14:F1} | {tThread,10:F1} | {tTask,10:F1} | {tSeq / tThread,13:F2} | {tSeq / tTask,11:F2}");
            }

            Console.WriteLine(new string('-', 106));

            int far = Array.IndexOf(res.dist, res.dist.Max());
            paths.Add($"n = {n}: " + PathToString(res.dist, res.prev, a, far));
            paths.Add($"n = {n}: " + PathToString(res.dist, res.prev, a, n - 1));
        }

        Console.WriteLine($"\nПеревірка (усі версії збігаються з Дейкстрою на купі): {(allCorrect ? "OK" : "ПОМИЛКА")}");

        Console.WriteLine("\nПриклади найкоротших шляхів:");
        foreach (var p in paths)
            Console.WriteLine(p);
    }
}
