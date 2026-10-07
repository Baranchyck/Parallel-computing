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
        const int MaxSegments = 20;   
        const int TickMs = 100;       

        List<Brush>[] segs = { new List<Brush>(), new List<Brush>(), new List<Brush>() };

        Queue<int>[] inbox = { new Queue<int>(), new Queue<int>(), new Queue<int>() };

        int[] idle = { 0, 0, 0 };          
        int[] fixedDur = { 2, 4, 8 };      
        volatile bool running = true;      
        volatile bool paused = false;      
        volatile bool randomMode = true;   
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

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(TickMs);
            timer.Tick += Timer_Tick;

            for (int i = 0; i < 3; i++) inbox[i].Enqueue(3);
        }

        void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            randomMode = chkRandom.IsChecked == true;   
            btnStart.IsEnabled = false;
            chkRandom.IsEnabled = false;
            btnPause.IsEnabled = true;
            timer.Start();

            for (int i = 0; i < 3; i++)
            {
                Thread t = new Thread(Work);
                t.IsBackground = true;   
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

        void Work(object o)
        {
            int id = (int)o;
            int next = (id + 1) % 3;   

            while (running)
            {
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

                for (int i = 0; i < ticks && running; i++)
                {
                    AddSeg(id, Brushes.Blue);
                    Tick();
                }

                lock (locker)
                {
                    inbox[next].Enqueue(randomMode ? rnd.Next(1, 6) : fixedDur[next]);
                }
            }
        }

        void Tick()
        {
            while (paused && running)
                Thread.Sleep(20);
            Thread.Sleep(TickMs);
        }

        void AddSeg(int id, Brush c)
        {
            lock (segs[id])
            {
                segs[id].Add(c);
                if (segs[id].Count > MaxSegments)
                    segs[id].RemoveAt(0);   
            }
        }

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