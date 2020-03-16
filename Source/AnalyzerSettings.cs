using System.ComponentModel;

namespace SpectrumAnalyzer
{
    public class AnalyzerSettings
    {
        [ReadOnly(true)]
        public int NumBands { get; set; }

        public int MinFreq { get; set; } = 0;
        public int MaxFreq { get; set; } = 400;

        private int _numBands;

        public AnalyzerSettings(int numBands)
        {
            NumBands = numBands;
        }
    }
}
