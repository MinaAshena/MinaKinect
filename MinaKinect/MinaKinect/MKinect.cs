using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using GraphDLL;

namespace MinaKinect
{
    public static partial class MKinect
    {
        private static KinectSensor _mKinect;

        #region Initialize
        static ColorFormat _colorFormat;
        static DepthFormat _depthFormat;

        public static void initKinect(ColorFormat colorFormat, DepthFormat depthFormat)
        {
            _mKinect = KinectSensor.GetDefault();

            _colorFormat = colorFormat;
            _depthFormat = depthFormat;

            _mKinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            _mKinect.Open();
        }

        public static void EnableSmoothSetting(int correction, int prediction, int smoothing, int jitterRadius, int maxDeviationRadius) { }

        #endregion

        #region CameraAngle
        public static int CameraVerticalAngle = 0;
        #endregion

        #region Color
        public static byte[] ColorMap { get; private set; }
       static byte[] frameBytesArray;
        public static Color GetColorPixel(int x, int y)
        {
            return GetColorPixel(x, y, ColorMap);
        }
        public static Color GetColorPixel(int x, int y, byte[] map)
        {
            if (_mKinect != null)
            {
                EnableColorStream();
                if (ColorMap != null && map != null)
                {
                    uint place = (uint)(y * _mKinect.ColorFrameSource.FrameDescription.Width + x) * 4;
                    return Color.FromRGB(map[place + 2], map[place + 1], map[place]);
                }
            }
            return Color.Black;
        }
        private static void EnableColorStream()
        {
            if (_mKinect.ColorFrameSource.IsActive == false)
            {
                frameBytesArray = new byte[_mKinect.ColorFrameSource.FrameDescription.LengthInPixels * 4];
                ColorFrameReader colorFrameReader = _mKinect.ColorFrameSource.OpenReader();
                colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
            }
        }

        static void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            
            using (ColorFrame acquireFrame = e.FrameReference.AcquireFrame())
            {
                if (acquireFrame != null)
                {
                    acquireFrame.CopyConvertedFrameDataToArray(frameBytesArray, ColorImageFormat.Bgra);
                    ColorMap = frameBytesArray;
                }
            }
        }
        #endregion
    }
}
