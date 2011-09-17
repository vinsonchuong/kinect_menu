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
    public partial class MainWindow : Window
    {
        #region Constants

        private static readonly Dictionary<String, IEnumerable<string>> MenuHierarchy =
            new Dictionary<string, IEnumerable<string>>
        {
            {
                "Root",
                new List<string>
                {
                    "Games",
                    "Movies",
                    "Music",
                    "Apps",
                    "Settings"
                }
            },
            {
                "Games",
                new List<string>
                {
                    "Dance Central",
                    "Kinectimals",
                    "Carnival Games",
                    "Kinect Sports",
                    "Kung Fu Panda",
                    "Angry Birds"
                }
            },
            {
                "Movies",
                new List<string>
                {
                    "Everything Must Go",
                    "Something Borrowed",
                    "Rango",
                    "Limitless",
                    "Rio",
                    "The Lincoln Lawyer"
                }
            },
            {
                "Music",
                new List<string>
                {
                    "Philipp Glass - The Hours",
                    "Novalima - Afro",
                    "Amon Tobin - Permutation"
                }
            },
            {
                "Philipp Glass - The Hours",
                new List<string>
                {
                    "The Poet Acts",
                    "Morning Passages",
                    "Something She Has to Do",
                    "For Your Own Benefit",
                    "Vanessa and the Changelings",
                    "I'm Going to Make a Cake",
                    "An Unwelcomed Friend"
                }
            },
            {
                "Novalima - Afro",
                new List<string>
                {
                    "Chinchivi",
                    "Bandolero",
                    "Malato",
                    "Machete",
                    "Candela"
                }
            },
            {
                "Amon Tobin - Permutation",
                new List<string>
                {
                    "Like Regular Chickens",
                    "Bridge",
                    "Reanimator",
                    "Sordid",
                    "Nightlife",
                    "Escape"
                }
            },
            {
                "Apps",
                new List<string>
                {
                    "Facebook",
                    "Google+",
                    "Twitter",
                    "Yelp"
                }
            }
        };

        #endregion Constants

        #region Instance Variables

        #endregion Instance Variables

        #region Initialization

        public MainWindow()
        {
            InitializeComponent();
            PushMenu(MenuHierarchy["Root"]);
        }

        #endregion Initialization

        #region Event Handlers

        private void HandleHover(double y)
        {
            var menu = GetActiveMenu();
            var item = SelectByY(y);
            if (item != null)
                menu.SelectedItem = item;
        }

        private void HandleLeftSwipe(double y)
        {
            HandleHover(y);
            var menu = GetActiveMenu();
            var item = (ListBoxItem) menu.SelectedItem;
            if (item != null)
                PushMenu(MenuHierarchy[(string)item.Content]);
        }

        private void HandleRightSwipe(double y)
        {
            PopMenu();
        }

        #endregion Event Handlers

        #region Menu Manipulation Helpers

        private ListBox GetActiveMenu()
        {
            return (ListBox)ActiveContainer.Children[0];
        }

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

        private ListBoxItem SelectByY(double y)
        {
            var menu = GetActiveMenu();
            return (
                from ListBoxItem item in menu.Items
                let top = PointToScreen(new Point(0, 0)).Y
                let bottom = top + item.ActualHeight
                where y >= top && y <= bottom select item
            ).FirstOrDefault();
        }

        #endregion
    }

}
