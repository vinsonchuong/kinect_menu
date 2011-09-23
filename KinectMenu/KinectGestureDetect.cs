using System;
using System.Linq;
using System.Windows;
using Kinect.Toolbox;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;

namespace KinectMenu
{
    class KinectGestureDetect
    {
        #region Instance Variables

        private Runtime KinectRuntime;

        private readonly SwipeGestureDetector SwipeGestureRecognizer;
        private readonly BarycenterHelper BarycenterHelper;
        private readonly AlgorithmicPostureDetector AlgorithmicPostureRecognizer;

        private Action<Point> LeftSwipeHandler;
        private Action<Point> RightSwipeHandler;
        private Action<Point> HoverHandler;

        // Position of rightHand
        private Point pt;

        #endregion Instance Variables

        #region Initialization

        public KinectGestureDetect(Action<Point> leftSwipeHandler, Action<Point> rightSwipeHandler,
            Action<Point> hoverHandler)
        {
            KinectRuntime = new Runtime();
            SwipeGestureRecognizer = new SwipeGestureDetector();
            BarycenterHelper = new BarycenterHelper();
            AlgorithmicPostureRecognizer = new AlgorithmicPostureDetector();

            LeftSwipeHandler = leftSwipeHandler;
            RightSwipeHandler = rightSwipeHandler;
            HoverHandler = hoverHandler;
        }

        public void KinectLoad()
        {
            KinectRuntime.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(ProcessSkeleton);

            SwipeGestureRecognizer.OnGestureDetected += OnGestureDetected;

            MakeSmoothMove();
        }

        #endregion Initialization

        private void MakeSmoothMove()
        {
            KinectRuntime.SkeletonEngine.TransformSmooth = true;

            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.75f,
                Correction = 0.1f,
                Prediction = 0.1f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };

            KinectRuntime.SkeletonEngine.SmoothParameters = parameters;
        }

        private void OnGestureDetected(string gesture)
        {
            if (gesture.Equals("SwipeToLeft"))
                LeftSwipeHandler(pt);
            else
                RightSwipeHandler(pt);
        }

        private void ProcessSkeleton(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (e.SkeletonFrame.Skeletons.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked).Count() > 0)
            {
                foreach (var skeleton in e.SkeletonFrame.Skeletons)
                {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        BarycenterHelper.Add(skeleton.Position.ToVector3(), skeleton.TrackingID);
                        if (BarycenterHelper.IsStable(skeleton.TrackingID))
                        {
                            foreach (Joint joint in skeleton.Joints)
                            {
                                if (joint.Position.W >= 0.8f && joint.TrackingState == JointTrackingState.Tracked &&
                                    joint.ID == JointID.HandRight)
                                {
                                    SwipeGestureRecognizer.Add(joint.Position, KinectRuntime.SkeletonEngine);
                                    var scaledJoint = joint.ScaleTo(1263, 681, .5f, .5f);
                                    HoverHandler(new Point(scaledJoint.Position.X, scaledJoint.Position.Y));
                                }
                            }
                            AlgorithmicPostureRecognizer.TrackPostures(skeleton);
                        }
                    }
                }
            }
        }
    }
}
