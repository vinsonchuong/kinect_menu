using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Kinect.Toolbox;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;

namespace KinectMenu
{
    class KinectGestureDetect
    {
        Runtime kinectRuntime;

        readonly ColorStreamManager streamManager;
        readonly SwipeGestureDetector swipeGestureRecognizer;
        readonly BarycenterHelper barycenterHelper;
        readonly AlgorithmicPostureDetector algorithmicPostureRecognizer;

        // SkeletonDisplayManager skeletonDisplayManager;

        Canvas kinectCanvas;
        Image kinectDisplay;
        Image kinectDepth;
  
        Action<Point> leftSwifeHandler;
        Action<Point> rightSwifeHandler;
        Action<Point> hoverHandler;

        // Position of rightHand
        Point pt = new Point(0,0);

        public KinectGestureDetect(Action<Point> leftSwifeHandler, Action<Point> rightSwifeHandler,
            Action<Point> hoverHandler, Canvas kinectCanvas, Image kinectDisplay, Image kinectDepth)
        {
            this.kinectRuntime = new Runtime();
            this.streamManager = new ColorStreamManager();
            this.swipeGestureRecognizer = new SwipeGestureDetector();
            this.barycenterHelper = new BarycenterHelper();
            this.algorithmicPostureRecognizer = new AlgorithmicPostureDetector();

            this.kinectCanvas = kinectCanvas;
            //this.gesturesCanvas = gesturesCanvas;
            this.kinectDisplay = kinectDisplay;
            this.kinectDepth = kinectDepth;

            //this.detectedGestures = detectedGestures;
            //this.rightHandPosition = rightHandPosition;

            this.leftSwifeHandler = leftSwifeHandler;
            this.rightSwifeHandler = rightSwifeHandler;
            this.hoverHandler = hoverHandler;
        }

        public void KinectLoad()
        {
            kinectRuntime.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(KinectRuntime_VideoFrameReady);
            kinectRuntime.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinectRuntime_SkeletonFrameReady);
            kinectRuntime.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(kinectRuntime_DepthFrameReady);

            kinectRuntime.Initialize(RuntimeOptions.UseDepth | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);

            kinectRuntime.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
            kinectRuntime.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.Depth);

            swipeGestureRecognizer.OnGestureDetected += OnGestureDetected;

            // skeletonDisplayManager = new SkeletonDisplayManager(kinectRuntime.SkeletonEngine, kinectCanvas);

            MakeSmoothMove();
        }

        public void MakeSmoothMove()
        {
            // Make movement smooth
            kinectRuntime.SkeletonEngine.TransformSmooth = true;

            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.75f,
                Correction = 0.1f,
                Prediction = 0.1f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };

            kinectRuntime.SkeletonEngine.SmoothParameters = parameters;
        }

        public void OnGestureDetected(string gesture)
        {
            // int pos = detectedGestures.Items.Add(string.Format("{0} : {1}", gesture, DateTime.Now));
            // detectedGestures.SelectedIndex = pos;
            if (gesture.Equals("SwipeToRight")){
                rightSwifeHandler(pt);
                // Console.WriteLine("SwipeToRight");
            }else if (gesture.Equals("SwipeToLeft")){
                leftSwifeHandler(pt);
                // Console.WriteLine("SwipeToLeft");
            }
        }

        public void kinectRuntime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (e.SkeletonFrame.Skeletons.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked).Count() == 0)
                return;
            
            ProcessFrame(e.SkeletonFrame);
        }

        public void ProcessFrame(SkeletonFrame frame)
        {
            foreach (var skeleton in frame.Skeletons)
            {
                if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                    continue;

                barycenterHelper.Add(skeleton.Position.ToVector3(), skeleton.TrackingID);

                // stabilities.Add(skeleton.TrackingID, barycenterHelper.IsStable(skeleton.TrackingID) ? "Stable" : "Unstable");
                if (!barycenterHelper.IsStable(skeleton.TrackingID))
                    continue;

                foreach (Joint joint in skeleton.Joints)
                {
                    if (joint.Position.W < 0.8f || joint.TrackingState != JointTrackingState.Tracked)
                        continue;

                    if (joint.ID == JointID.HandRight)
                    {
                        swipeGestureRecognizer.Add(joint.Position, kinectRuntime.SkeletonEngine);

                        // Get position of joint and Save it to global variable
                        setRightHandPosition(joint);
                    }
                }
                algorithmicPostureRecognizer.TrackPostures(skeleton);
            }
            // skeletonDisplayManager.Draw(frame);
        }

        public void setRightHandPosition(Joint joint)
        {
            int windowX = 1263 / 2;
            int windowY = 681 / 2;

            float scaledX;
            float scaledY;

            if (joint.Position.X >= 0) // from 0 to 1; Right Half
            {
                scaledX = (windowX) + (windowX * joint.Position.X);
            }
            else // from -1 to 0; Left Half
            {
                scaledX = (windowX) * (1 - Math.Abs(joint.Position.X));
            }

            // Adjust the range of cursor's position within windows
            if (joint.Position.Y >= 0) // from 1 to 0
            {
                scaledY = (windowY + 200) * (1 - joint.Position.Y);
            }
            else // from 0 to -1
            {
                scaledY = (windowY + 200) + ((windowY) * Math.Abs(joint.Position.Y));
            }

            hoverHandler(new Point(scaledX, scaledY));

            // var scaledJoint = joint.ScaleTo(1263, 681, .5f, .5f);
            // hoverHandler(new Point(scaledX, scaledY));
        }

        public void KinectRuntime_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            kinectDisplay.Source = streamManager.Update(e);
        }

        public void kinectRuntime_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            kinectDepth.Source = e.ImageFrame.ToBitmapSource();
        }

        public void KinectClose()
        {
            kinectRuntime.Uninitialize();
        }
    }
}
