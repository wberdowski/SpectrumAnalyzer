using System;
using System.ComponentModel;

namespace SpectrumAnalyzer
{
    public class AnalyzerSettings
    {
        [Description("Number of bands processed by FFT.")]
        public FftBands NumBands
        {
            get
            {
                return numBands;
            }
            set
            {
                numBands = value;
                exponent = (int)Math.Truncate(Math.Log((int)numBands, 2));
                BandsNumberChanged?.Invoke(this, null);
            }
        }

        public int Exponent
        {
            get
            {
                return exponent;
            }
        }

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

        private FftBands numBands = FftBands.Fft1024;
        private int exponent = 10;
        private int minFreq = 0;
        private int maxFreq = 20000;

        public event EventHandler<EventArgs> BandsNumberChanged;
        public event EventHandler<EventArgs> FrequencyRangeChanged;

        public AnalyzerSettings()
        {
        }
    }
}
