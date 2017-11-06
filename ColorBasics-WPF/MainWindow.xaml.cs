//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Net;
    using System.Collections.Generic;
    using System.Linq;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string MyIP { get; set; }

        private const int MapDepthToByte = 8000 / 256;

        private KinectSensor kinectSensor = null;

        private ColorFrameReader colorFrameReader = null;
        private DepthFrameReader depthFrameReader = null;

        private WriteableBitmap colorBitmap = null;
        private WriteableBitmap depthBitmap = null;

        private FrameDescription depthFrameDescription = null;
        private FrameDescription colorFrameDescription;

        private byte[] colorPixels = null;
        private ushort[] rawDepth = null;
        private byte[] depthPixels = null;

        CameraSpacePoint[] cameraSpaceDepthData;
        ColorSpacePoint[] colorSpaceDepthData;

        private string statusText = null;

        private readonly ServerCommunication communicationServer;

        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();

            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            this.depthFrameReader.FrameArrived += this.Reader_FrameArrived;

            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            this.rawDepth = new ushort[this.depthFrameDescription.Width * this.depthFrameDescription.Height];
            this.cameraSpaceDepthData = new CameraSpacePoint[rawDepth.Length];
            this.colorSpaceDepthData = new ColorSpacePoint[rawDepth.Length];

            this.colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.colorPixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * 2];

            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            this.kinectSensor.Open();

            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            this.DataContext = this;

            this.communicationServer = new ServerCommunication(GetDepthDataForNetwork, GetRgbData);
            this.communicationServer.Start();

            MyIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList[3].ToString();

            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        public ImageSource DepthImageSource
        {
            get
            {
                return this.depthBitmap;
            }
        }

        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyRawFrameDataToArray(colorPixels);

                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();
                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));

                        }
                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }
        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];
                this.rawDepth[i] = depth;
                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private byte[] GetDepthDataForNetwork()
        {
            lock (rawDepth)
            {
                this.kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(rawDepth, cameraSpaceDepthData);
                this.kinectSensor.CoordinateMapper.MapDepthFrameToColorSpace(rawDepth, colorSpaceDepthData);

                return GetNetworkDataAsBytes(cameraSpaceDepthData, colorSpaceDepthData);
            }
        }
        private byte[] GetRgbData()
        {
            lock (colorPixels)
            {
                return colorPixels;
            }
        }

        private byte[] GetNetworkDataAsBytes(CameraSpacePoint[] cameraSpaceData, ColorSpacePoint[] colorData)
        {

            byte[] pixelDataHolder = new byte[5 * sizeof(float) * colorData.Length];
            for (int i = 0; i < colorData.Length; i++)
            {
                CameraSpacePoint cameraPoint = cameraSpaceData[i];
                ColorSpacePoint colorPoint = colorData[i];
                GetPixelData(i, cameraPoint, colorPoint, pixelDataHolder, i * 5 * sizeof(float));
            }
            return pixelDataHolder;
        }

        private void GetPixelData(int pixelIndex, CameraSpacePoint cameraPoint, ColorSpacePoint colorPoint, byte[] pixelData, int startIdx)
        {

            Array.Copy(BitConverter.GetBytes(cameraPoint.X), 0, pixelData, startIdx + 0 * sizeof(float), sizeof(float));
            Array.Copy(BitConverter.GetBytes(cameraPoint.Y), 0, pixelData, startIdx + 1 * sizeof(float), sizeof(float));
            Array.Copy(BitConverter.GetBytes(cameraPoint.Z), 0, pixelData, startIdx + 2 * sizeof(float), sizeof(float));
            Array.Copy(BitConverter.GetBytes(colorPoint.X), 0, pixelData, startIdx + 3 * sizeof(float), sizeof(float));
            Array.Copy(BitConverter.GetBytes(colorPoint.Y), 0, pixelData, startIdx + 4 * sizeof(float), sizeof(float));
        }
    }
}
