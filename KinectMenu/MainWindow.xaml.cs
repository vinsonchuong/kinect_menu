using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Kinect.Toolbox;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;

namespace KinectMenu
{
    public partial class MainWindow : Window
    {
        #region Constants

        private static readonly Dictionary<String, IEnumerable<string>> MenuHierarchy =
            new Dictionary<string, IEnumerable<string>>
        {
            {"Root", new string[] {"Games", "Movies", "Music", "Apps", "Settings"}},
            {"Games", new string[] {"Dance Central", "Kinectimals", "Carnival Games", "Kinect Sports", "Kung Fu Panda", "Angry Birds"}},
            {"Movies", new string[] {"Everything Must Go", "Something Borrowed", "Rango", "Limitless", "Rio", "The Lincoln Lawyer"}},
            {"Music", new string[] {"Philipp Glass - The Hours", "Novalima - Afro", "Amon Tobin - Permutation"}},
            {"Philipp Glass - The Hours", new string[] {"The Poet Acts", "Morning Passages", "Something She Has to Do", "For Your Own Benefit", "Vanessa and the Changelings", "I'm Going to Make a Cake", "An Unwelcomed Friend"}},
            {"Novalima - Afro", new string[] {"Chinchivi", "Bandolero", "Malato", "Machete", "Candela"}},
            {"Amon Tobin - Permutation", new string[] {"Like Regular Chickens", "Bridge", "Reanimator", "Sordid", "Nightlife", "Escape"}},
            {"Apps", new string[] {"Facebook", "Google+", "Twitter", "Yelp"}}
        };

        private const string RootName = "Root";

        #endregion Constants

        #region Instance Variables

        private readonly Runtime KinectRuntime;
        private readonly ColorStreamManager ColorStreamManager;

        private readonly Dictionary<string, ListBox> Menus;
        private ListBox CurrentMenu;
        private readonly Stack<ListBox> Breadcrumb;

        #endregion Instance Variables

        #region Initialization

        public MainWindow()
        {
            InitializeComponent();

            KinectRuntime = new Runtime();
            ColorStreamManager = new ColorStreamManager();
            InitializeKinect();

            Menus = new Dictionary<string, ListBox>();
            Breadcrumb = new Stack<ListBox>();
            InitializeMenu();
        }

        private void InitializeKinect()
        {
            KinectRuntime.VideoFrameReady += (object sender, ImageFrameReadyEventArgs e) =>
            {
                KinectVideo.Source = ColorStreamManager.Update(e);
            };
            KinectRuntime.DepthFrameReady += (object sender, ImageFrameReadyEventArgs e) =>
            {
                KinectDepth.Source = e.ImageFrame.ToBitmapSource();
            };
            KinectGestureDetector.Initialize(KinectRuntime, HandleLeftSwipe, HandleRightSwipe, HandleHover);
            KinectRuntime.Initialize(RuntimeOptions.UseDepth | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
//            KinectRuntime.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
//            KinectRuntime.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.Depth);

            KinectRuntime.SkeletonEngine.TransformSmooth = true;
            KinectRuntime.SkeletonEngine.SmoothParameters = new TransformSmoothParameters
            {
                Smoothing = 0.75f,
                Correction = 0.1f,
                Prediction = 0.1f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };
        }

        private void InitializeMenu()
        {
            foreach (var name in MenuHierarchy.Keys)
                Menus[name] = MakeMenu(MenuHierarchy[name], HandleMenuClick);
            PushMenu(RootName);
        }

        private void Close(object sender, EventArgs e)
        {
            KinectRuntime.Uninitialize();
        }

        #endregion Initialization

        #region Mouse Event Handlers

        private void HandleMenuClick(object sender, MouseEventArgs e)
        {
            PushMenu((ListBoxItem)sender);
            e.Handled = true;
        }

        private void HandleBreadcrumbClick(object sender, MouseEventArgs e)
        {
            PopMenu();
            e.Handled = true;
        }

        #endregion Mouse Event Handlers

        #region Kinect Event Handlers

        private void HandleHover(Point pt)
        {
            CursorImage.Margin = new Thickness(pt.X, pt.Y, 0, 0);
            var item = SelectByY(pt);
            if (item != null)
                CurrentMenu.SelectedItem = item;
            else
                CurrentMenu.SelectedIndex = -1;
        }

        private void HandleLeftSwipe(Point pt)
        {
            var item = (ListBoxItem)CurrentMenu.SelectedItem;
            if (item != null)
                PushMenu(item);
        }

        private void HandleRightSwipe(Point pt)
        {
            PopMenu();
        }

        #endregion Event Handlers

        #region Menu Manipulation Helpers

        private ListBox MakeMenu(IEnumerable<string> items, MouseButtonEventHandler clickHandler)
        {
            var menu = new ListBox();
            foreach (string item in items)
            {
                var node = new ListBoxItem { Content = item };
                node.MouseUp += clickHandler;
                menu.Items.Add(node);
            }
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new TranslateTransform(0, 0));
            transformGroup.Children.Add(new ScaleTransform(1, 1, 0.5, 0.5));
            menu.RenderTransform = transformGroup;
            MenuContainer.Children.Add(menu);
            return menu;
        }

        private void PushMenu(string title)
        {
            if (CurrentMenu != null && (CurrentMenu != Menus[RootName] || Menus.ContainsKey(title)))
            {
                CurrentMenu.SelectedIndex = -1;
                Breadcrumb.Push(CurrentMenu);
                BreadcrumbContainer.Items.Add(new ListBoxItem { Content = title });
                Minimize(CurrentMenu);
            }
            if (Menus.ContainsKey(title))
            {
                CurrentMenu = Menus[title];
            }
            else
            {
                CurrentMenu = Menus[RootName];
                Breadcrumb.Clear();
                BreadcrumbContainer.Items.Clear();
            }
            New(CurrentMenu);
        }

        private void PushMenu(ListBoxItem selectedItem)
        {
            PushMenu((string)selectedItem.Content);
        }

        private void PopMenu()
        {
            if (Breadcrumb.Count > 0)
            {
                Discard(CurrentMenu);
                BreadcrumbContainer.Items.RemoveAt(BreadcrumbContainer.Items.Count - 1);
                CurrentMenu = Breadcrumb.Pop();
                Restore(CurrentMenu);
            }
        }

        private ListBoxItem SelectByY(Point pt)
        {
            return (
                from ListBoxItem item in CurrentMenu.Items
                let top = item.TranslatePoint(new Point(0, 0), Window).Y
                let left = item.TranslatePoint(new Point(0, 0), Window).X
                let bottom = top + item.ActualHeight
                let right = left + item.ActualWidth
                where ((left <= pt.X && pt.X <= right) && (top <= pt.Y && pt.Y <= bottom))
                select item
            ).FirstOrDefault();
        }

        #endregion Menu Manipulation Helpers

        #region Menu Animations

        private void Minimize(ListBox menu)
        {
            menu.IsEnabled = false;
            var breadcrumbItem = (ListBoxItem)BreadcrumbContainer.Items[BreadcrumbContainer.Items.Count - 1];
            Point
                origin = new Point(0, 0),
                menuPoint = menu.TranslatePoint(origin, this),
                breadcrumbPoint = breadcrumbItem.TranslatePoint(origin, this)
            ;
            var duration = new Duration(TimeSpan.FromMilliseconds(300));
            DoubleAnimation
                posXAnimation = new DoubleAnimation { Duration = duration, To = 2.8 * (breadcrumbPoint.X - menuPoint.X) },
                posYAnimation = new DoubleAnimation { Duration = duration, To = 2.8 * (breadcrumbPoint.Y - menuPoint.Y) },
                scaleXAnimation = new DoubleAnimation { Duration = duration, To = 0.3 },
                scaleYAnimation = new DoubleAnimation { Duration = duration, To = 0.3 },
                opacityAnimation = new DoubleAnimation { Duration = duration, To = 0 }
            ;
            var translateTransform = ((TransformGroup)menu.RenderTransform).Children[0];
            var scaleTransform = ((TransformGroup)menu.RenderTransform).Children[1];
            translateTransform.BeginAnimation(TranslateTransform.XProperty, posXAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, posYAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
            opacityAnimation.Completed += (object sender, EventArgs e) =>
            {
                menu.Visibility = Visibility.Collapsed;
                menu.IsEnabled = true;
                translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                menu.BeginAnimation(OpacityProperty, null);
            };
            menu.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void Restore(ListBox menu)
        {
            menu.IsEnabled = false;
            menu.Visibility = Visibility.Visible;
            var breadcrumbItem = BreadcrumbContainer.Items.Count > 0
                ? (ListBoxItem)BreadcrumbContainer.Items[BreadcrumbContainer.Items.Count - 1]
                : null;
            Point
                origin = new Point(0, 0),
                menuPoint = menu.TranslatePoint(origin, this),
                breadcrumbPoint = breadcrumbItem == null ? origin : breadcrumbItem.TranslatePoint(origin, this)
            ;
            var duration = new Duration(TimeSpan.FromMilliseconds(300));
            DoubleAnimation
                posXAnimation = new DoubleAnimation { Duration = duration, From = breadcrumbPoint.X - menuPoint.X },
                posYAnimation = new DoubleAnimation { Duration = duration, From = breadcrumbPoint.Y - menuPoint.Y },
                scaleXAnimation = new DoubleAnimation { Duration = duration, From = 0.3 },
                scaleYAnimation = new DoubleAnimation { Duration = duration, From = 0.3 },
                opacityAnimation = new DoubleAnimation { Duration = duration, From = 0 }
            ;
            var translateTransform = ((TransformGroup)menu.RenderTransform).Children[0];
            var scaleTransform = ((TransformGroup)menu.RenderTransform).Children[1];
            translateTransform.BeginAnimation(TranslateTransform.XProperty, posXAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, posYAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
            opacityAnimation.Completed += (object sender, EventArgs e) =>
            {
                menu.IsEnabled = true;
                translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                menu.BeginAnimation(OpacityProperty, null);
            };
            menu.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void New(ListBox menu)
        {
            menu.IsEnabled = false;
            menu.SelectedIndex = -1;
            menu.Visibility = Visibility.Visible;
            var duration = new Duration(TimeSpan.FromMilliseconds(400));
            DoubleAnimation
                posXAnimation = new DoubleAnimation { Duration = duration, From = -80 },
                posYAnimation = new DoubleAnimation { Duration = duration, From = -80 },
                scaleXAnimation = new DoubleAnimation { Duration = duration, From = 1.5 },
                scaleYAnimation = new DoubleAnimation { Duration = duration, From = 1.5 },
                opacityAnimation = new DoubleAnimation { Duration = duration, From = 0 }
            ;
            var translateTransform = ((TransformGroup)menu.RenderTransform).Children[0];
            var scaleTransform = ((TransformGroup)menu.RenderTransform).Children[1];
            translateTransform.BeginAnimation(TranslateTransform.XProperty, posXAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, posYAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
            opacityAnimation.Completed += (object sender, EventArgs e) =>
            {
                menu.IsEnabled = true;
                translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                menu.BeginAnimation(OpacityProperty, null);
            };
            menu.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void Discard(ListBox menu)
        {
            menu.IsEnabled = false;
            var duration = new Duration(TimeSpan.FromMilliseconds(400));
            DoubleAnimation
                posXAnimation = new DoubleAnimation { Duration = duration, To = -80 },
                posYAnimation = new DoubleAnimation { Duration = duration, To = -80 },
                scaleXAnimation = new DoubleAnimation { Duration = duration, To = 1.5 },
                scaleYAnimation = new DoubleAnimation { Duration = duration, To = 1.5 },
                opacityAnimation = new DoubleAnimation { Duration = duration, To = 0 }
            ;
            var translateTransform = ((TransformGroup)menu.RenderTransform).Children[0];
            var scaleTransform = ((TransformGroup)menu.RenderTransform).Children[1];
            translateTransform.BeginAnimation(TranslateTransform.XProperty, posXAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, posYAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
            opacityAnimation.Completed += (object sender, EventArgs e) =>
            {
                menu.Visibility = Visibility.Collapsed;
                menu.IsEnabled = true;
                translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                menu.BeginAnimation(OpacityProperty, null);
            };
            menu.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        #endregion Menu Animations
    }
}
