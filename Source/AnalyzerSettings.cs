using System;
using System.ComponentModel;

namespace SpectrumAnalyzer
{
    public class AnalyzerSettings
    {
        [ReadOnly(true)]
        [Description("Number of bands processed by FFT.")]
        public int NumBands { get; set; }

        [Description("The minimum frequency in Hz shown in the graph.")]
        public int MinFreq
        {
            get
            {
                return minFreq;
            }
            set
            {
                minFreq = value;
                FrequencyRangeChanged?.Invoke(this, null);
            }
        }

        [Description("The maximum frequency in Hz shown in the graph.")]
        public int MaxFreq
        {
            get
            {
                return maxFreq;
            }
            set
            {
                maxFreq = value;
                FrequencyRangeChanged?.Invoke(this, null);
            }
        }

        private int minFreq = 0;
        private int maxFreq = 20000;

        public event EventHandler<EventArgs> FrequencyRangeChanged;

        public AnalyzerSettings(int numBands)
        {
            NumBands = numBands;
        }
    }
}
