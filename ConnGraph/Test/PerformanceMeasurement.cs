using System;
using System.Diagnostics;

namespace Connectivity.test
{
    /// <summary>
    /// A Stopwatch wrapper that can provide total, average and worst time.
    /// </summary>
    public class PerformanceMeasurement
    {
        private Stopwatch _swTotal = new Stopwatch();
        private Stopwatch _swWorst = new Stopwatch();

        public PerformanceMeasurement(TimeSpan threshold)
        {
            Threshold = threshold;
        }

        public PerformanceMeasurement(double msThreshold)
        {
            Threshold = TimeSpan.FromMilliseconds(msThreshold);
        }

        public PerformanceMeasurement() : this(1)
        {
        }

        public TimeSpan Total => _swTotal.Elapsed;
        public TimeSpan Worst { get; private set; } = TimeSpan.Zero;

        public TimeSpan Average => TimeSpan.FromTicks(Times != 0 ? _swTotal.ElapsedTicks / Times : -1);
        public long Times { get; private set; }
        public long Exceeding { get; private set; }
        public TimeSpan Threshold { get; set; }

        public override string ToString()
        {
            return string.Format("Total {0,9:###0.0000} , Worst {1,9:0000.0000} , Average {2,6:0.0000} , Times {3} , Exceeding {4}",
                Total.TotalMilliseconds,
                Worst.TotalMilliseconds,
                Average.TotalMilliseconds,
                Times, Exceeding);
        }

        public void Start()
        {
            Times++;
            _swWorst.Restart();
            _swTotal.Start();
        }

        public void Stop()
        {
            _swTotal.Stop();
            _swWorst.Stop();

            var last = _swWorst.Elapsed;
            if (last > Worst)
            {
                Worst = last;
            }

            if (last > Threshold)
            {
                Exceeding++;
            }
        }

        public void Reset()
        {
            Times = 0;
            Worst = TimeSpan.Zero;
            _swTotal.Reset();
        }

        public void Restart()
        {
            Reset();
            Start();
        }
    }
}