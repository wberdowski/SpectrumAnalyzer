using System.ComponentModel;

namespace SpectrumAnalyzer
{
    public class AnalyzerSettings
    {
        [ReadOnly(true)]
        public int NumBands { get; set; }

        public int MinFreq { get; set; } = 0;
        public int MaxFreq { get; set; } = 2000;

        public AnalyzerSettings(int numBands)
        {
            NumBands = numBands;
        }
    }
}
