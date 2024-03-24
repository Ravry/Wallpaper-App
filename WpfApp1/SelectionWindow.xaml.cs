using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XamlAnimatedGif;

namespace WpfApp1
{
    public partial class SelectionWindow : Window
    {
        private System.Windows.Forms.NotifyIcon mNotifyIcon;

        public SelectionWindow()
        {
            InitializeComponent();
            
            mNotifyIcon = new System.Windows.Forms.NotifyIcon();
            mNotifyIcon.BalloonTipText = "click to show!";
            mNotifyIcon.BalloonTipTitle = "Wallpaper";
            mNotifyIcon.Text = "Wallpaper Selector";
            mNotifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            mNotifyIcon.Click += (sender, ev) =>
            {
                if (this.WindowState == WindowState.Normal)
                    this.WindowState = WindowState.Minimized;
                else if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }
            };
            mNotifyIcon.Visible = true;
            

            this.Width = (2120 / 10) * 4;
            this.Height = (1080 / 10) * 4; 

            this.Loaded += (sender, ev) =>
            {
                string[] files = Directory.GetFiles("C:\\Wallpaper");

                WrapPanel wallpapersPanel = (WrapPanel)FindName("WallpapersPanel");

                foreach (string file in files)
                {
                    string fileType = file.Split('.')[1];
                    bool isGif = (fileType != "gif" && fileType != "png" && fileType != "jpg" && fileType != "jpeg") ? false : true;

                    Button dynamicButton = new Button();


                    if (isGif)
                    {
                        Image image1 = new Image();
                        image1.Source = new BitmapImage(new Uri(file));
                        image1.Stretch = Stretch.UniformToFill;
                        dynamicButton.Content = image1;
                    }
                    else 
                        dynamicButton.Content = file;
                    dynamicButton.Width = (1920 / 11);
                    dynamicButton.Height = (1080 / 11);
                    dynamicButton.Padding = new Thickness(0);
                    dynamicButton.Margin = new Thickness(5);

                    dynamicButton.Click += (s, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (isGif)
                                MainWindow.playGif(file);
                            else
                                MainWindow.playVideo(file);

                            //var targetWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                            //if (targetWindow != null)
                            //{
                            //    var childrenCopy = targetWindow.canvas.Children.OfType<Image>().ToList();

                            //    foreach (var gif in childrenCopy)
                            //    {
                            //        AnimationBehavior.SetSourceUri(gif, new Uri(file));
                            //    }
                            //}
                        });
                    };
                    wallpapersPanel.Children.Add(dynamicButton);
                }

                ExitBtn.Click += (s, e) =>
                {
                    Environment.Exit(0);
                };
            };

            this.MouseDown += (sender, ev) => {
                if (ev.ChangedButton == MouseButton.Left)
                    this.DragMove();
            };

            this.Closed += (sender, ev) => {
                Environment.Exit(0);
            };
        }
    }
}
