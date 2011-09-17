using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectMenu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        #region Constants

        KinectGestureDetect kgd;

        private static readonly OrderedDictionary MenuHierarchy = new OrderedDictionary
        {
            {
                "Games",
                new OrderedDictionary
                {
                    {"Dance Central", null},
                    {"Kinectimals", null},
                    {"Carnival Games", null},
                    {"Kinect Sports", null},
                    {"Kung Fu Panda", null},
                    {"Angry Birds", null}
                }
            },
            {
                "Movies",
                new OrderedDictionary
                {
                    {"Everything Must Go", null},
                    {"Something Borrowed", null},
                    {"Rango", null},
                    {"Limitless", null},
                    {"Rio", null},
                    {"The Lincoln Lawyer", null}
                }
            },
            {
                "Music",
                new OrderedDictionary
                {
                    {
                        "Philipp Glass - The Hours",
                        new OrderedDictionary
                        {
                            {"The Poet Acts", null},
                            {"Morning Passages", null},
                            {"Something She Has to Do", null},
                            {"For Your Own Benefit", null},
                            {"Vanessa and the Changelings", null},
                            {"I'm Going to Make a Cake", null},
                            {"An Unwelcomed Friend", null}
                        }
                    },
                    {
                        "Novalima - Afro",
                        new OrderedDictionary
                        {
                            {"Chinchivi", null},
                            {"Bandolero", null},
                            {"Malato", null},
                            {"Machete", null},
                            {"Candela", null}
                        }
                    },
                    {
                        "Amon Tobin - Permutation",
                        new OrderedDictionary
                        {
                            {"Like Regular Chickens", null},
                            {"Bridge", null},
                            {"Reanimator", null},
                            {"Sordid", null},
                            {"Nightlife", null},
                            {"Escape", null}
                        }
                    }
                }
            },
            {
                "Apps",
                new OrderedDictionary
                {
                    {"Facebook", null},
                    {"Google+", null},
                    {"Twitter", null},
                    {"Yelp", null}
                }
            },
            {"Settings", null}
        };

        #endregion Constants

        #region Instance Variables

        #endregion Instance Variables

        #region Initialization


        public MainWindow()
        {
            InitializeComponent();
            Console.WriteLine(MenuHierarchy.Keys.Cast<string>());
            PushMenu(MenuHierarchy.Keys.Cast<string>());
        }

        #endregion Initialization

        #region Menu Manipulation Helpers

        private void PushMenu(IEnumerable<string> items)
        {
            var menu = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            foreach (string item in items)
            {
                var node = new ListBoxItem
                {
                    Content = item,
                    Background = Brushes.White,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 10),
                    Padding = new Thickness(10),
                    FontSize = 32.0
                };
                menu.Items.Add(node);
            }

            if (ActiveContainer.Children.Count > 0)
            {
                var oldMenu = ActiveContainer.Children[0];
                ActiveContainer.Children.Clear();
                BreadcrumbContainer.Children.Add(oldMenu);
            }
            ActiveContainer.Children.Add(menu);
        }

        private void PopMenu()
        {
            var popIndex = BreadcrumbContainer.Children.Count - 1;
            var menu = BreadcrumbContainer.Children[popIndex];
            BreadcrumbContainer.Children.RemoveAt(popIndex);
            ActiveContainer.Children.Clear();
            ActiveContainer.Children.Add(menu);
        }

        public void SelectByY(double y)
        {
            var menu = (ListBox)ActiveContainer.Children[0];
            Func<ListBoxItem, bool> isHit = (item) =>
            {
                var top = PointToScreen(new Point(0, 0)).Y;
                var bottom = top + item.ActualHeight;
                return y >= top && y <= bottom;
            };
            // var selected = (from item in menu where isHit((ListBoxItem)item) select (ListBoxItem)item).First(
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try{
                
                kgd = new KinectGestureDetect();
                
                kgd.KinectLoad();

            }catch(Exception ex){
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            kgd.KinectClose();
        }
    }
}
