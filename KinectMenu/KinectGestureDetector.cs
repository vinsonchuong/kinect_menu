using System;
using System.Linq;
using System.Windows;
using Kinect.Toolbox;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;

namespace KinectMenu
{
    class KinectGestureDetector
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
        private Point pt = new Point(0,0);

        #endregion Instance Variables

        #region Initialization

        public static void Initialize(Runtime kinectRuntime, Action<Point> leftSwipeHandler,
            Action<Point> rightSwipeHandler, Action<Point> hoverHandler)
        {
            new KinectGestureDetector(kinectRuntime, leftSwipeHandler, rightSwipeHandler, hoverHandler);
        }

        private KinectGestureDetector(Runtime kinectRuntime, Action<Point> leftSwipeHandler,
            Action<Point> rightSwipeHandler, Action<Point> hoverHandler)
        {
            KinectRuntime = kinectRuntime;
            SwipeGestureRecognizer = new SwipeGestureDetector();
            BarycenterHelper = new BarycenterHelper();
            AlgorithmicPostureRecognizer = new AlgorithmicPostureDetector();

            LeftSwipeHandler = leftSwipeHandler;
            RightSwipeHandler = rightSwipeHandler;
            HoverHandler = hoverHandler;
            KinectRuntime.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(ProcessSkeleton);
            SwipeGestureRecognizer.OnGestureDetected += OnGestureDetected;
        }

        #endregion Initialization

        #region Frame Handling

        private void OnGestureDetected(string gesture)
        {
            if (gesture.Equals("SwipeToLeft"))
                LeftSwipeHandler(pt);
            else
                RightSwipeHandler(pt);
        }

        private void ProcessSkeleton(object sender, SkeletonFrameReadyEventArgs e)
        {
            foreach (var skeleton in e.SkeletonFrame.Skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked))
            {
                BarycenterHelper.Add(skeleton.Position.ToVector3(), skeleton.TrackingID);
                if (BarycenterHelper.IsStable(skeleton.TrackingID))
                {
                    foreach (Joint joint in skeleton.Joints)
                    {
                        if (
                            joint.Position.W >= 0.8f &&
                            joint.TrackingState == JointTrackingState.Tracked &&
                            joint.ID == JointID.HandRight
                        )
                        {
                            SwipeGestureRecognizer.Add(joint.Position, KinectRuntime.SkeletonEngine);
                            SetPosition(joint);
                        }
                    }
                    AlgorithmicPostureRecognizer.TrackPostures(skeleton);
                }
            }
        }

        private void SetPosition(Joint joint)
        {
            int windowX = 1263 / 2;
            int windowY = 681 / 2;
            float scaledX;
            float scaledY;

            if (joint.Position.X >= 0) // from 0 to 1; Right Half
                scaledX = (windowX) + (windowX * joint.Position.X);
            else // from -1 to 0; Left Half
                scaledX = (windowX) * (1 - Math.Abs(joint.Position.X));

            // Adjust the range of cursor's position within windows
            if (joint.Position.Y >= 0) // from 1 to 0
                scaledY = (windowY + 200) * (1 - joint.Position.Y);
            else // from 0 to -1
                scaledY = (windowY + 200) + ((windowY) * Math.Abs(joint.Position.Y));

            HoverHandler(new Point(scaledX, scaledY));
        }

        #endregion Frame Handling
    }
}
