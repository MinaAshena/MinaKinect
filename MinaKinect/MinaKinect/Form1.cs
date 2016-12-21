using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Kinect;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MinaKinect
{
    public partial class Form1 : Form
    {
        KinectSensor sensor;
        CoordinateMapper mapper;
        Bitmap colorBitmap;
        Graphics colorBitmapGraphics;
        Rectangle colorRectangle;
        Skeleton[] skeletonData;
        SkeletonPoint fingerTipLeft, fingerTipRight, hair;
        long lastColorFrameTime;
        int colorFPS;
        double heightSkeletonHands, heightSkeletonHeadFeet, heightFingerTip, heightHair;
        const int jointSize = 5;

        double Distance(SkeletonPoint p1, SkeletonPoint p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2) + Math.Pow(p2.Z - p1.Z, 2));
        }

        void WriteOnScreen()
        {
            label1.Text =
                "\nFPS : " + colorFPS +
                "\nHeight (Skeleton Hands) : " + (int)heightSkeletonHands +
                "\nHeight (Skeleton Head to Feet) : " + (int)heightSkeletonHeadFeet +
                "\nHeight (Depth by Fingertips) : " + (int)heightFingerTip +
                "\nHeight (Depth by TopHair) : " + (int)heightHair;
        }

        void DrawSkeletonPoint(SkeletonPoint point)
        {
            var colorPoint = mapper.MapSkeletonPointToColorPoint(point, sensor.ColorStream.Format);
            colorBitmapGraphics.FillEllipse(Brushes.Blue, colorPoint.X - jointSize, colorPoint.Y - jointSize, 2 * jointSize, 2 * jointSize);
        }

        void DrawSkeletons()
        {
            foreach (Skeleton skeleton in skeletonData)
                if (skeleton != null && skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    DrawSkeletonPoint(skeleton.Joints[JointType.HandLeft].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.WristLeft].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.ElbowLeft].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.ShoulderLeft].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.ShoulderCenter].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.ShoulderRight].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.ElbowRight].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.WristRight].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.HandRight].Position);

                    DrawSkeletonPoint(skeleton.Joints[JointType.Head].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.Spine].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.HipCenter].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.HipLeft].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.KneeLeft].Position);
                    DrawSkeletonPoint(skeleton.Joints[JointType.FootLeft].Position);

                    DrawSkeletonPoint(fingerTipLeft);
                    DrawSkeletonPoint(fingerTipRight);
                    DrawSkeletonPoint(hair);
                }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (KinectSensor.KinectSensors.Count == 0)
            {
                MessageBox.Show("No kinect sensor is connected.");
                Close();
                return;
            }

            sensor = KinectSensor.KinectSensors[0];

            sensor.ColorFrameReady += Sensor_ColorFrameReady;
            sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;
            sensor.DepthFrameReady += Sensor_DepthFrameReady;

            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.SkeletonStream.Enable();
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.Start();

            mapper = new CoordinateMapper(sensor);
            colorRectangle = new Rectangle(0, 0, sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight);
            colorBitmap = new Bitmap(colorRectangle.Width, colorRectangle.Height);
            skeletonData = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];
            colorBitmapGraphics = Graphics.FromImage(colorBitmap);

            label1.Size = colorRectangle.Size;
            label1.Image = colorBitmap;
        }

        private void Sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            try
            {
                DepthImagePixel[] depthImagePixel;
                using (var depthFrame = e.OpenDepthImageFrame())
                {
                    if (depthFrame == null) return;
                    depthImagePixel = depthFrame.GetRawPixelData();
                }

                fingerTipLeft.X = int.MaxValue;
                fingerTipRight.X = int.MinValue;
                hair.Y = int.MaxValue;
                for (int i = 0; i < depthImagePixel.Length; i++)
                {
                    DepthImagePixel pixel = depthImagePixel[i];
                    if (pixel.IsKnownDepth && pixel.PlayerIndex > 0)
                    {
                        var point = new DepthImagePoint()
                        {
                            X = i % sensor.DepthStream.FrameWidth,
                            Y = i / sensor.DepthStream.FrameWidth,
                            Depth = pixel.Depth,
                        };

                        if (point.X < fingerTipLeft.X)
                            fingerTipLeft = mapper.MapDepthPointToSkeletonPoint(sensor.DepthStream.Format, point);
                        if (point.X > fingerTipRight.X)
                            fingerTipRight = mapper.MapDepthPointToSkeletonPoint(sensor.DepthStream.Format, point);
                        if (point.Y < hair.Y)
                            hair = mapper.MapDepthPointToSkeletonPoint(sensor.DepthStream.Format, point);
                    }
                }

                heightFingerTip = Distance(fingerTipLeft, fingerTipRight) * 100;
            }
            catch { }
        }

        private void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            try
            {
                using (var skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (skeletonFrame == null) return;
                    skeletonFrame.CopySkeletonDataTo(skeletonData);
                }

                foreach (Skeleton skeleton in skeletonData)
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        heightSkeletonHands =
                            (Distance(skeleton.Joints[JointType.HandLeft].Position, skeleton.Joints[JointType.WristLeft].Position) +
                            Distance(skeleton.Joints[JointType.WristLeft].Position, skeleton.Joints[JointType.ElbowLeft].Position) +
                            Distance(skeleton.Joints[JointType.ElbowLeft].Position, skeleton.Joints[JointType.ShoulderLeft].Position) +
                            Distance(skeleton.Joints[JointType.ShoulderLeft].Position, skeleton.Joints[JointType.ShoulderCenter].Position) +
                            Distance(skeleton.Joints[JointType.ShoulderCenter].Position, skeleton.Joints[JointType.ShoulderRight].Position) +
                            Distance(skeleton.Joints[JointType.ShoulderRight].Position, skeleton.Joints[JointType.ElbowRight].Position) +
                            Distance(skeleton.Joints[JointType.ElbowRight].Position, skeleton.Joints[JointType.WristRight].Position) +
                            Distance(skeleton.Joints[JointType.WristRight].Position, skeleton.Joints[JointType.HandRight].Position)) * 100;

                        heightSkeletonHeadFeet =
                            Distance(skeleton.Joints[JointType.Head].Position, skeleton.Joints[JointType.FootLeft].Position) * 100;

                        heightHair = Distance(hair, skeleton.Joints[JointType.FootLeft].Position) * 100;

                        WriteOnScreen();
                        break;
                    }
            }
            catch { }
        }

        private void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            try
            {
                byte[] rawPixelData;

                using (var colorImageFrame = e.OpenColorImageFrame())
                {
                    if (colorImageFrame == null) return;
                    rawPixelData = colorImageFrame.GetRawPixelData();
                }

                var bitmapData = colorBitmap.LockBits(colorRectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
                Marshal.Copy(rawPixelData, 0, bitmapData.Scan0, rawPixelData.Length);
                colorBitmap.UnlockBits(bitmapData);

                colorFPS = (int)(10000000 / (DateTime.Now.Ticks - lastColorFrameTime));
                WriteOnScreen();
                DrawSkeletons();
                lastColorFrameTime = DateTime.Now.Ticks;
            }
            catch { }
        }
    }
}
