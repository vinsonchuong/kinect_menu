using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        SkeletonDisplayManager skeletonDisplayManager;

        Canvas kinectCanvas;
        //Canvas gesturesCanvas;
        //Image kinectDisplay;
        //ListBox detectedGestures;
        //TextBlock rightHandPosition;
  
        Action<double> leftSwifeHandler;
        Action<double> rightSwifeHandler;
        Action<double> hoverHandler;

        float rightHandY;

        public KinectGestureDetect(Action<double> leftSwifeHandler, Action<double> rightSwifeHandler, Action<double> hoverHandler, Canvas kinectCanvas)
        {
            this.kinectRuntime = new Runtime();
            this.streamManager = new ColorStreamManager();
            this.swipeGestureRecognizer = new SwipeGestureDetector();
            this.barycenterHelper = new BarycenterHelper();
            this.algorithmicPostureRecognizer = new AlgorithmicPostureDetector();

            this.kinectCanvas = kinectCanvas;
            //this.gesturesCanvas = gesturesCanvas;
            //this.kinectDisplay = kinectDisplay;
            //this.detectedGestures = detectedGestures;
            //this.rightHandPosition = rightHandPosition;

            this.leftSwifeHandler = leftSwifeHandler;
            this.rightSwifeHandler = rightSwifeHandler;
            this.hoverHandler = hoverHandler;
        }

        public void KinectLoad()
        {
            // kinectRuntime.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(KinectRuntime_VideoFrameReady);
            kinectRuntime.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinectRuntime_SkeletonFrameReady);

            kinectRuntime.Initialize(RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);

            kinectRuntime.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);

            swipeGestureRecognizer.OnGestureDetected += OnGestureDetected;

            skeletonDisplayManager = new SkeletonDisplayManager(kinectRuntime.SkeletonEngine, kinectCanvas);

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
            //int pos = detectedGestures.Items.Add(string.Format("{0} : {1}", gesture, DateTime.Now));
            //detectedGestures.SelectedIndex = pos;
            if (gesture.Equals("SwipeToRight")){
                rightSwifeHandler(rightHandY);
                Console.WriteLine("SwipeToRight");
            }else if (gesture.Equals("SwipeToLeft")){
                leftSwifeHandler(rightHandY);
                Console.WriteLine("SwipeToLeft");
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

                        // Get position of joint
                        setRightHandPosition(joint);
                    }
                }
                algorithmicPostureRecognizer.TrackPostures(skeleton);
            }
            skeletonDisplayManager.Draw(frame);
        }

        public void setRightHandPosition(Joint joint)
        {
            Joint aJoint = joint.ScaleTo(1280, 720, 0.5f, 0.5f);
            rightHandY = aJoint.Position.Y;
            Console.WriteLine("Y: " + rightHandY);
            hoverHandler(rightHandY);
        }

        //public void KinectRuntime_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        //{
        //    kinectDisplay.Source = streamManager.Update(e);
        //}

        public void KinectClose()
        {
            kinectRuntime.Uninitialize();
        }
    }
}
