using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Utils;
using System;
using System.Windows.Forms;

namespace SpectrumAnalyzer
{
    public partial class FormMain : Form
    {
        private AnalyzerSettings mSettings;
        private WasapiLoopbackCapture mWasapi;
        private int _sampleOffset;
        private Complex[] _storedSamples;

        public FormMain()
        {
            InitializeComponent();
            Application.ApplicationExit += Application_ApplicationExit;
            Load += FormMain_Load;

            // Setup property grid
            mSettings = new AnalyzerSettings(1024 * 8);
            mSettings.FrequencyRangeChanged += (s, e) =>
            {
                chart1.ChartAreas[0].AxisX.Minimum = mSettings.MinFreq;
                chart1.ChartAreas[0].AxisX.Maximum = mSettings.MaxFreq;
            };

            _storedSamples = new Complex[mSettings.NumBands];
            propertyGrid1.SelectedObject = mSettings;


            // Setup chart

            for (int i = 0; i < mSettings.NumBands; i++)
            {
                chart1.Series[0].Points.AddXY(i, 0);
            }

            chart1.ChartAreas[0].AxisY.Maximum = 0.1D;
            chart1.ChartAreas[0].AxisY.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Minimum = mSettings.MinFreq;
            chart1.ChartAreas[0].AxisX.Maximum = mSettings.MaxFreq;


        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            mWasapi = new WasapiLoopbackCapture(10);
            mWasapi.DataAvailable += Wasapi_DataAvailable;
            mWasapi.Initialize();
            mWasapi.Start();

            // Status
            var format = mWasapi.Device.DeviceFormat;
            toolStripStatusLabel1.Text = $"WaveFormat: {format.SampleRate}Hz, {format.BitsPerSample} bit, {format.Channels} channel(s)";
        }

        private void Wasapi_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            float[] samples = PrepareSamples(e.Data, e.ByteCount);

            int channels = 2;
            for (int i = 0; i < samples.Length; i += channels)
            {
                float s = MergeSamples(samples, i, channels);
                _storedSamples[_sampleOffset] = new Complex(s, 0f);

                _sampleOffset += 1;

                if (_sampleOffset >= _storedSamples.Length)
                {
                    _sampleOffset = 0;
                }
            }

            float[] fft = new float[mSettings.NumBands];

            // Compute FFT and store it in the buffer
            ComputeFft(fft);

            try
            {
                if (!IsDisposed && !Disposing)
                    Invoke((MethodInvoker)delegate ()
                    {
                        for (int a = 0; a < fft.Length; a++)
                        {
                            double amplitude = fft[a];

                            chart1.Series[0].Points[a].SetValueXY(BarToFreq(a), amplitude);
                        }

                        chart1.Invalidate();
                    });
            }
            catch (ObjectDisposedException)
            {

            }
        }

        private float[] PrepareSamples(byte[] buffer, int len)
        {
            float[] result = new float[len / sizeof(float)];
            Buffer.BlockCopy(buffer, 0, result, 0, len);

            return result;
        }

        private float MergeSamples(float[] samples, int index, int channelCount)
        {
            float z = 0f;
            for (int i = 0; i < channelCount; i++)
            {
                z += samples[index + i];
            }
            return z / channelCount;
        }

        private void ComputeFft(float[] resultBuffer)
        {
            Complex[] input = new Complex[_storedSamples.Length];

            _storedSamples.CopyTo(input, 0);

            FastFourierTransformation.Fft(input, (int)Math.Truncate(Math.Log(mSettings.NumBands, 2)));

            for (int i = 0; i <= input.Length / 2 - 1; i++)
            {
                var z = input[i];
                resultBuffer[i] = (float)z.Value;
            }
        }


        private int FreqToBar(float frequency)
        {
            return (int)(frequency * mSettings.NumBands / mWasapi.WaveFormat.SampleRate);
        }

        private float BarToFreq(int bar)
        {
            return bar * (float)mWasapi.WaveFormat.SampleRate / mSettings.NumBands;
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            mWasapi.DataAvailable -= Wasapi_DataAvailable;
            mWasapi.Stop();
            mWasapi.Dispose();
        }
    }
}
