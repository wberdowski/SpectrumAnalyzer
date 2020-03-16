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

        public FormMain()
        {
            InitializeComponent();
            Application.ApplicationExit += Application_ApplicationExit;
            Load += FormMain_Load;

            // Setup property grid
            mSettings = new AnalyzerSettings();
            mSettings.BandsNumberChanged += (s, e) =>
            {
                UpdateChartPoints();
            };
            mSettings.FrequencyRangeChanged += (s, e) =>
            {
                chart1.ChartAreas[0].AxisX.Minimum = mSettings.MinFreq;
                chart1.ChartAreas[0].AxisX.Maximum = mSettings.MaxFreq;
            };

            propertyGrid1.SelectedObject = mSettings;

            // Setup chart
            UpdateChartPoints();

            chart1.ChartAreas[0].AxisY.Maximum = 0.1D;
            chart1.ChartAreas[0].AxisY.Minimum = 0;

            chart1.ChartAreas[0].AxisX.Minimum = mSettings.MinFreq;
            chart1.ChartAreas[0].AxisX.Maximum = mSettings.MaxFreq;
        }

        private void UpdateChartPoints()
        {
            chart1.Series[0].Points.Clear();
            for (int i = 0; i < (int)mSettings.NumBands; i++)
            {
                chart1.Series[0].Points.AddXY(i, 0);
            }
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
            _sampleOffset = 0;
            try
            {
                Complex[] _storedSamples = new Complex[(int)mSettings.NumBands];

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

                float[] fft = new float[(int)mSettings.NumBands];

                // Compute FFT and store it in the buffer
                fft = ComputeFft(_storedSamples);


                if (!IsDisposed && !Disposing)
                    Invoke((MethodInvoker)delegate ()
                    {
                        try
                        {
                            for (int a = 0; a < fft.Length; a++)
                            {
                                double amplitude = fft[a] * (mSettings.Exponent - 9);

                                chart1.Series[0].Points[a].SetValueXY(BarToFreq(a), amplitude);
                            }

                            chart1.Invalidate();
                        } catch (Exception)
                        {

                        }
                    });
            }
            catch (Exception)
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

        private float[] ComputeFft(Complex[] samples)
        {
            float[] resultBuffer = new float[samples.Length / 2];
            FastFourierTransformation.Fft(samples, mSettings.Exponent);

            for (int i = 0; i < samples.Length / 2; i++)
            {
                var z = samples[i];
                resultBuffer[i] = (float)z.Value;
            }

            return resultBuffer;
        }


        private int FreqToBar(float frequency)
        {
            return (int)(frequency * (int)mSettings.NumBands / (float)mWasapi.WaveFormat.SampleRate);
        }

        private float BarToFreq(int bar)
        {
            return bar * mWasapi.WaveFormat.SampleRate / (float)mSettings.NumBands;
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            mWasapi.DataAvailable -= Wasapi_DataAvailable;
            mWasapi.Stop();
            mWasapi.Dispose();
        }
    }
}
