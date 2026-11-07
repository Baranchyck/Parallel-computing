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

    class MstState
    {
        public int[] Key = [];       
        public int[] Parent = [];   
        public bool[] InTree = [];   
    }

    static MstState Init(int n, int a)
    {
        var s = new MstState
        {
            Key = new int[n],
            Parent = new int[n],
            InTree = new bool[n]
        };
        Array.Fill(s.Key, INF);
        Array.Fill(s.Parent, -1);
        s.Key[a] = 0;
        return s;
    }

    static void UpdateKeys(Graph g, MstState s, int u, int from, int to)
    {
        for (int e = g.Start[u]; e < g.Start[u + 1]; e++)
        {
            int v = g.Adj[e];
            if (v < from || v >= to || s.InTree[v]) continue;
            if (g.W[e] < s.Key[v])
            {
                s.Key[v] = g.W[e];
                s.Parent[v] = u;
            }
        }
    }

    static (int key, int v) MinInBlock(MstState s, int from, int to)
    {
        int best = INF, bestV = -1;
        for (int v = from; v < to; v++)
            if (!s.InTree[v] && s.Key[v] < best)
            {
                best = s.Key[v];
                bestV = v;
            }
        return (best, bestV);
    }

    static int ArgMin((int key, int v)[] local)
    {
        var best = (key: INF, v: -1);
        foreach (var x in local)
            if (x.key < best.key) best = x;
        return best.v;
    }

    static int[] PrimSequential(Graph g, int a)
    {
        int n = g.N;
        var s = Init(n, a);

        for (int iter = 0; iter < n; iter++)
        {
            int u = MinInBlock(s, 0, n).v;
            if (u == -1) break;
            s.InTree[u] = true;
            UpdateKeys(g, s, u, 0, n);
        }

        return s.Parent;
    }

    static int[] PrimThreads(Graph g, int a, int k)
    {
        int n = g.N;
        var s = Init(n, a);
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
                        UpdateKeys(g, s, u, from, to);

                    var buf = local[iter & 1];
                    buf[id] = MinInBlock(s, from, to);
                    barrier.SignalAndWait();

                    u = ArgMin(buf);
                    if (u == -1) break;
                    if (u >= from && u < to) s.InTree[u] = true;
                }
            });
            threads[t].Start();
        }

        foreach (var th in threads)
            th.Join();

        barrier.Dispose();

        return s.Parent;
    }

    static long KruskalWeight(Graph g)
    {
        var edges = new List<(int w, int u, int v)>(g.M);
        for (int u = 0; u < g.N; u++)
            for (int e = g.Start[u]; e < g.Start[u + 1]; e++)
                if (u < g.Adj[e]) edges.Add((g.W[e], u, g.Adj[e]));
        edges.Sort();

        var dsu = Enumerable.Range(0, g.N).ToArray();
        int Find(int x)
        {
            while (dsu[x] != x) x = dsu[x] = dsu[dsu[x]];
            return x;
        }

        long total = 0;
        foreach (var (w, u, v) in edges)
        {
            int ru = Find(u), rv = Find(v);
            if (ru == rv) continue;
            dsu[ru] = rv;
            total += w;
        }
        return total;
    }

    static int EdgeWeight(Graph g, int u, int v)
    {
        int best = INF;
        for (int e = g.Start[u]; e < g.Start[u + 1]; e++)
            if (g.Adj[e] == v) best = Math.Min(best, g.W[e]);
        return best;
    }

    static long TreeWeight(Graph g, int[] parent, int a)
    {
        long total = 0;
        for (int v = 0; v < g.N; v++)
        {
            if (v == a) { if (parent[v] != -1) return -1; continue; }
            if (parent[v] == -1) return -1;
            int w = EdgeWeight(g, v, parent[v]);
            if (w == INF) return -1;
            total += w;
        }
        return total;
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

    static void PrintDemo()
    {
        const int n = 8, a = 3;
        var g = GenerateGraph(n, 4, 7);

        Console.WriteLine($"Приклад: n = {n}, m = {g.M}, стартова вершина a{a}");
        Console.WriteLine("Ребра графа (u - v : вага):");
        for (int u = 0; u < n; u++)
            for (int e = g.Start[u]; e < g.Start[u + 1]; e++)
                if (u < g.Adj[e])
                    Console.WriteLine($"  a{u} - a{g.Adj[e]} : {g.W[e]}");

        var seq = PrimSequential(g, a);
        var par = PrimThreads(g, a, 4);

        Console.WriteLine("Мінімальне кісткове дерево (k = 4 потоки):");
        for (int v = 0; v < n; v++)
            if (par[v] != -1)
                Console.WriteLine($"  a{par[v]} - a{v} : {EdgeWeight(g, v, par[v])}");

        Console.WriteLine($"Вага дерева: {TreeWeight(g, par, a)} " +
                          $"(послідовно: {TreeWeight(g, seq, a)}, Крускал: {KruskalWeight(g)})\n");
    }

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        List<int> n_list = [1000, 5000, 10000, 20000, 50000, 100000];
        List<int> k_list = [2, 4, 8, 16, 32, 64];
        const int avgDegree = 10;

        var warm = GenerateGraph(500, avgDegree, 1);
        PrimSequential(warm, 0);
        PrimThreads(warm, 0, 2);

        PrintDemo();

        var rnd = new Random(2026);

        Console.WriteLine($"Логічних ядер: {Environment.ProcessorCount}");
        Console.WriteLine($"Граф: неорієнтований, зв'язний, ваги 1..99, середній степінь вершини {avgDegree}\n");
        Console.WriteLine("     n |       m |      a | Вага MST |  k | Послідовно, мс | Thread, мс | Прискорення | Ефективність");
        Console.WriteLine(new string('-', 104));

        bool allCorrect = true; 

        foreach (int n in n_list)
        {
            var g = GenerateGraph(n, avgDegree, 42);
            int a = rnd.Next(n);
            int repeats = n <= 10000 ? 3 : 1;

            var (seqParent, tSeq) = Measure(() => PrimSequential(g, a), repeats);
            long reference = KruskalWeight(g);
            long weight = TreeWeight(g, seqParent, a);
            allCorrect &= weight == reference;

            foreach (int k in k_list)
            {
                var (parParent, tThread) = Measure(() => PrimThreads(g, a, k), repeats);
                allCorrect &= TreeWeight(g, parParent, a) == reference;

                double speedup = tSeq / tThread;
                Console.WriteLine($"{n,6} | {g.M,7} | {"a" + a,6} | {weight,8} | {k,2} | {tSeq,14:F1} | {tThread,10:F1} | {speedup,11:F2} | {speedup / k,12:F3}");
            }

            Console.WriteLine(new string('-', 104));
        }

        Console.WriteLine($"\nПеревірка (вага дерева в усіх версіях = вага за Крускалом): {(allCorrect ? "OK" : "ПОМИЛКА")}");
    }
}
