using Accord.Math;
using CSCore.DSP;
using CSCore.SoundIn;
using System;
using System.Numerics;
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

            // Setup property grid
            mSettings = new AnalyzerSettings(1024 * 8);
            _storedSamples = new Complex[mSettings.NumBands];
            propertyGrid1.SelectedObject = mSettings;


            // Setup chart

            for (int i = 0; i < (int)mSettings.NumBands; i++)
            {
                chart1.Series[0].Points.AddXY(i, 0);
            }

            chart1.ChartAreas[0].AxisY.Maximum = 200;
            chart1.ChartAreas[0].AxisY.Minimum = 0;

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
            int i = 0;
            while (i < samples.Length)
            {
                float s = MergeSamples(samples, i, channels);
                _storedSamples[_sampleOffset] = new Complex(s, 0D);

                _sampleOffset += 1;

                if (_sampleOffset >= _storedSamples.Length)
                {
                    _sampleOffset = 0;
                }
                i += channels;
            }

            for (int x = 0; x < _storedSamples.Length; x++)
            {
                _storedSamples[x] *= FastFourierTransformation.HammingWindowF(i, _storedSamples.Length);
            }

            float[] fft = new float[mSettings.NumBands];

            CalculateFft(fft);

            try
            {
                if (!IsDisposed && !Disposing)
                    Invoke((MethodInvoker)delegate ()
                    {
                        for (int a = 0; a < fft.Length; a++)
                        {
                            double amplitude = fft[a] * 1000;

                            chart1.Series[0].Points[a].SetValueXY(BarToFreq(a), amplitude);
                        }


                        chart1.ChartAreas[0].AxisX.Minimum = mSettings.MinFreq;
                        chart1.ChartAreas[0].AxisX.Maximum = mSettings.MaxFreq;
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
            if (channelCount == 1)
            {
                return samples[index];
            }
            if (channelCount == 2)
            {
                return (samples[index] + samples[index + 1]) / 2f;
            }
            else
            {
                float z = 0f;
                for (int i = 0; i <= channelCount - 1; i++)
                {
                    z += samples[index + i];
                }
                return z / 2f;
            }
        }

        private void CalculateFft(float[] resultBuffer)
        {
            Complex[] input = new Complex[_storedSamples.Length];
            _storedSamples.CopyTo(input, 0);


            FourierTransform.FFT(input, FourierTransform.Direction.Forward);
            for (int i = 0; i <= input.Length / 2 - 1; i++)
            {
                var z = input[i];
                resultBuffer[i] = (float)z.Magnitude;
            }
        }


        private int FreqToBar(float frequency)
        {
            return (int)(frequency / (float)mWasapi.WaveFormat.SampleRate * (int)mSettings.NumBands) / 2;
        }

        private float BarToFreq(int bar)
        {
            return (bar * (float)mWasapi.WaveFormat.SampleRate / (int)mSettings.NumBands);
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            mWasapi.DataAvailable -= Wasapi_DataAvailable;
            mWasapi.Stop();
            mWasapi.Dispose();
        }
    }
}
