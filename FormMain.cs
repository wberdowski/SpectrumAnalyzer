using CSCore.DSP;
using CSCore.SoundIn;
using System;
using System.Windows.Forms;

namespace SpectrumAnalyzer
{
    public partial class FormMain : Form
    {
        private FftProvider fftProvider;
        private WasapiLoopbackCapture wasapi;
        private const int numBars = 1024 * 8;
        private const int bytesPerPoint = 4;
        private int sampleRate;
        private int maxBar;

        public FormMain()
        {
            InitializeComponent();
            Application.ApplicationExit += Application_ApplicationExit;

            // Setup chart

            for (int i = 0; i < numBars / 4; i++)
            {
                chart1.Series[0].Points.AddXY(i, 0);
            }
            chart1.ChartAreas[0].AxisY.Maximum = 60;
            chart1.ChartAreas[0].AxisY.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Maximum = 20000;

            fftProvider = new FftProvider(1, (FftSize)numBars);

            wasapi = new WasapiLoopbackCapture(10);
            wasapi.DataAvailable += Wasapi_DataAvailable;
            wasapi.Initialize();
            wasapi.Start();

            // Status
            var format = wasapi.WaveFormat;
            sampleRate = format.SampleRate;
            toolStripStatusLabel1.Text = $"WaveFormat: {format.SampleRate}Hz, {format.BitsPerSample} bits, {format.Channels} channel(s)";

            maxBar = FreqToBar(20000);
            Console.WriteLine(maxBar);
        }

        private void Wasapi_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            int graphPoints = e.ByteCount / bytesPerPoint;
            float[] pcm = new float[graphPoints];
            float[] fft = new float[graphPoints];
            float[] fftReal = new float[graphPoints / 2];

            for (int i = 0; i < graphPoints; i++)
            {
                var val = BitConverter.ToSingle(e.Data, i * bytesPerPoint);
                pcm[i] = val;// * FastFourierTransformation.HammingWindowF(i, graphPoints);
            }

            fftProvider.Add(pcm, pcm.Length);

            float[] fftBuffer = new float[numBars];

            if (fftProvider.GetFftData(fftBuffer))
            {
                try
                {
                    if (!IsDisposed && !Disposing)
                        Invoke((MethodInvoker)delegate ()
                        {
                            float maxX = 0;
                            float maxY = 0;

                            for (int i = 0; i < maxBar; i++)
                            {
                                float amplitude = fftBuffer[i] * 1000;

                                if (amplitude > maxY)
                                {
                                    maxY = amplitude;
                                    maxX = BarToFreq(i);
                                }

                                chart1.Series[0].Points[i].SetValueXY(BarToFreq(i), amplitude);
                            }

                            chart1.Invalidate();
                            toolStripStatusLabel2.Text = $"Peak: {maxX} Hz";
                        });
                }
                catch (ObjectDisposedException)
                {

                }
            }
        }

        private int FreqToBar(float frequency)
        {
            return (int)(frequency / (float)sampleRate * numBars) / 2;
        }

        private float BarToFreq(int bar)
        {
            return (bar * (float)sampleRate / numBars) * 2;
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            wasapi.DataAvailable -= Wasapi_DataAvailable;
            wasapi.Stop();
            wasapi.Dispose();
        }
    }
}
