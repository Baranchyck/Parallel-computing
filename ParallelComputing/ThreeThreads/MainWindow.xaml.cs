using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ThreeThreads
{
    public partial class MainWindow : Window
    {
        const int MaxSegments = 20;   // скільки сегментів влазить у «циліндр»
        const int TickMs = 100;       // тривалість одного «тіка»

        // --- спільні дані потоків ---
        List<Brush>[] segs = { new List<Brush>(), new List<Brush>(), new List<Brush>() };

        // «поштові скриньки»: черга завдань (тривалостей) для кожного потоку
        Queue<int>[] inbox = { new Queue<int>(), new Queue<int>(), new Queue<int>() };

        int[] idle = { 0, 0, 0 };          // скільки тіків простою у кожного
        int[] fixedDur = { 3, 5, 2 };      // фіксовані інтервали для режиму без рандому
        volatile bool running = true;      // false = усі потоки завершуються
        volatile bool paused = false;      // true = усі потоки стоять на паузі
        volatile bool randomMode = true;   // режим інтервалів (фіксується при Start)
        object locker = new object();
        Random rnd = new Random();

        Canvas[] canvases;
        TextBlock[] labels;
        DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();

            canvases = new Canvas[] { canvas1, canvas2, canvas3 };
            labels = new TextBlock[] { lbl1, lbl2, lbl3 };

            // таймер у UI-потоці: оновлює малюнок
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(TickMs);
            timer.Tick += Timer_Tick;

            // стартові завдання: початковий час однаковий для всіх потоків.
            // Щоб побачити «двоє працюють, один відпочиває», закоментуй
            // цикл і використай два рядки нижче:
            for (int i = 0; i < 3; i++) inbox[i].Enqueue(3);
            // inbox[0].Enqueue(3);
            // inbox[1].Enqueue(3);
        }

        // ---------- кнопки ----------
        void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            randomMode = chkRandom.IsChecked == true;   // читаємо галочку тут, в UI-потоці
            btnStart.IsEnabled = false;
            chkRandom.IsEnabled = false;
            btnPause.IsEnabled = true;
            timer.Start();

            for (int i = 0; i < 3; i++)
            {
                Thread t = new Thread(Work);
                t.IsBackground = true;   // не тримає процес після закриття вікна
                t.Start(i);
            }
        }

        void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            paused = !paused;
            btnPause.Content = paused ? "Resume" : "Pause";
        }

        void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            running = false;
        }

        // ---------- робота одного потоку ----------
        void Work(object o)
        {
            int id = (int)o;
            int next = (id + 1) % 3;   // 0 -> 1 -> 2 -> 0

            while (running)
            {
                // 1. чекаємо завдання. Простій = червоні сегменти
                int ticks = 0;
                bool got = false;
                while (running && !got)
                {
                    lock (locker)
                    {
                        if (inbox[id].Count > 0)
                        {
                            ticks = inbox[id].Dequeue();
                            got = true;
                        }
                    }
                    if (!got)
                    {
                        AddSeg(id, Brushes.Red);
                        Interlocked.Increment(ref idle[id]);
                        Tick();
                    }
                }
                if (!running) break;

                // 2. працюємо. Робота = сині сегменти
                for (int i = 0; i < ticks && running; i++)
                {
                    AddSeg(id, Brushes.Blue);
                    Tick();
                }

                // 3. передаємо наступному потоку, скільки йому працювати
                lock (locker)
                {
                    inbox[next].Enqueue(randomMode ? rnd.Next(1, 6) : fixedDur[next]);
                }
            }
        }

        // один «тік»: якщо пауза, стоїмо; потім звичайна затримка.
        // Викликається поза lock, тому пауза нікого не блокує.
        void Tick()
        {
            while (paused && running)
                Thread.Sleep(20);
            Thread.Sleep(TickMs);
        }

        // додати сегмент у колонку потоку (UI тут не чіпаємо!)
        void AddSeg(int id, Brush c)
        {
            lock (segs[id])
            {
                segs[id].Add(c);
                if (segs[id].Count > MaxSegments)
                    segs[id].RemoveAt(0);   // колонка «їде» вгору
            }
        }

        // ---------- UI: тік таймера, перемальовуємо колонки ----------
        void Timer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 3; i++)
            {
                Draw(i);
                labels[i].Text = "Простій: " + idle[i];
            }
        }

        void Draw(int id)
        {
            Canvas canvas = canvases[id];
            int h = (int)canvas.Height / MaxSegments;

            canvas.Children.Clear();
            lock (segs[id])
            {
                for (int i = 0; i < segs[id].Count; i++)
                {
                    Rectangle r = new Rectangle();
                    r.Width = canvas.Width;
                    r.Height = h - 1;
                    r.Fill = segs[id][i];
                    Canvas.SetTop(r, i * h);
                    canvas.Children.Add(r);
                }
            }
        }
    }
}