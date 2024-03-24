using System;
using System.Text;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using DrawBehindDesktopIcons;
using XamlAnimatedGif;
using System.Drawing;
using System.Threading;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class MainWindow : System.Windows.Window
    {
        public static MainWindow Instance { get; private set; }

        private static List<System.Windows.Controls.Image> gifAndImageSourcePlayers = new List<System.Windows.Controls.Image>();
        private static List<MediaElement> videoSourcePlayers = new List<MediaElement>();

        public static IntPtr workerw = IntPtr.Zero;
        private bool currentWindowFocused = isWindowFocused();

        public void findWorkerW()
        {
            PrintVisibleWindowHandles(2);

            IntPtr progman = W32.FindWindow("Progman", null);
            Trace.WriteLine($"program = {progman:8X}");

            IntPtr result = IntPtr.Zero;

            W32.SendMessageTimeout(progman,
                                   0x052C,
                                   new IntPtr(0),
                                   IntPtr.Zero,
                                   W32.SendMessageTimeoutFlags.SMTO_NORMAL,
                                   1000,
                                   out result);

            Trace.WriteLine(result);

            PrintVisibleWindowHandles(2);
            workerw = IntPtr.Zero;

            W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = W32.FindWindowEx(tophandle,
                                            IntPtr.Zero,
                                            "SHELLDLL_DefView",
                                            IntPtr.Zero);

                if (p != IntPtr.Zero)
                {
                    workerw = W32.FindWindowEx(IntPtr.Zero,
                                               tophandle,
                                               "WorkerW",
                                               IntPtr.Zero);
                    
                    Trace.WriteLine($"{workerw.ToInt64():X8}");
                }

                return true;        
            }), IntPtr.Zero);
        }

        public MainWindow()
        {
            while (workerw == IntPtr.Zero)
            {
                Trace.WriteLine("WorkerW window not found after sending message to Progman.");
                Thread.Sleep(1000);
                findWorkerW();
            }

            InitializeComponent();

            Instance = this;
            this.Width = System.Windows.SystemParameters.VirtualScreenWidth;
            this.Height = System.Windows.SystemParameters.VirtualScreenHeight;

            IntPtr dc = W32.GetDCEx(workerw, IntPtr.Zero, (W32.DeviceContextValues)0x403);
            if (dc != IntPtr.Zero)
            {
                this.Loaded += (s, e) =>
                {
                    canvas.Width = this.Width;
                    canvas.Height = this.Height;



                    DispatcherTimer frameTimer = new DispatcherTimer();
                    frameTimer.Interval = TimeSpan.FromSeconds(1.0 / 4.0); ; // Assuming 60 frames per second
                    frameTimer.Tick += (sender, ev) => { 
                        if (!isWindowFocused())
                        {
                            if (currentWindowFocused)
                            {
                                foreach (var element in videoSourcePlayers)
                                    element.Play();

                                currentWindowFocused = false;
                            }
                        }
                        else
                        {
                            if (!currentWindowFocused)
                            {
                                foreach (var element in videoSourcePlayers)
                                    element.Pause();

                                currentWindowFocused = true;
                            }
                        }
                    };
                    frameTimer.Start();

                    Screen[] screens = Screen.AllScreens;

                    int xMin = int.MaxValue;
                    int yMin = int.MaxValue;

                    foreach (var screen in screens)
                    {
                        Rectangle bounds = screen.Bounds;

                        Trace.WriteLine($"bounds {{ ({bounds.X} , {bounds.Y}) | ({bounds.Width} , {bounds.Height}) }}");

                        TransformGroup transformGroup = new TransformGroup();
                        transformGroup.Children.Add(new TranslateTransform() { X = bounds.Left + bounds.Width, Y = bounds.Top });

                        //create Image for playing gifs
                        System.Windows.Controls.Image gif = new System.Windows.Controls.Image();
                        gif.SetValue(AnimationBehavior.SourceUriProperty, new Uri(@"C:/Wallpaper/wp2.gif"));
                        gif.SetValue(AnimationBehavior.CacheFramesInMemoryProperty, true);
                        gif.Stretch = Stretch.Fill;
                        gif.RenderTransform = transformGroup;
                        gif.Width = bounds.Width;
                        gif.Height = bounds.Height;
                        gif.IsEnabled = false;
                        canvas.Children.Add(gif);
                        gifAndImageSourcePlayers.Add(gif);

                        //create mediaElement for playing videos
                         MediaElement mediaPlayer = new MediaElement();
                         mediaPlayer.CacheMode = new BitmapCache();
                         mediaPlayer.IsMuted = true;
                         mediaPlayer.Stretch = Stretch.Fill;
                         mediaPlayer.LoadedBehavior = MediaState.Manual;
                         mediaPlayer.RenderTransform = transformGroup;
                         mediaPlayer.MediaEnded += (se, ev) =>
                         {
                             mediaPlayer.Position = TimeSpan.Zero;
                             mediaPlayer.Play();
                         };
                        canvas.Children.Add(mediaPlayer);
                        videoSourcePlayers.Add(mediaPlayer);

                        if ((bounds.Left + bounds.Width) < xMin)
                            xMin = bounds.Left + bounds.Width;

                        if (bounds.Top < yMin) 
                            yMin = bounds.Top;
                    }

                    this.Left = xMin;
                    this.Top = yMin;

                    Thread th = new Thread(() =>
                    {
                        SelectionWindow selectionWindow = new SelectionWindow();
                        selectionWindow.Show();
                        System.Windows.Threading.Dispatcher.Run();
                    });
                    th.SetApartmentState(ApartmentState.STA);
                    th.Start();

                    playGif(@"C:/Wallpaper/wp4.gif");

                    this.Topmost = false;
                    W32.SetParent(new WindowInteropHelper(this).Handle, workerw);
                    this.Show();
                };
            }
        }

        public static void playVideo(string path)
        {
            foreach (var gifElement in gifAndImageSourcePlayers)
            {
                gifElement.IsEnabled = false;
                gifElement.Visibility = System.Windows.Visibility.Hidden;
            }

            Uri uri = new Uri(path);

            foreach (var videoElement in videoSourcePlayers)
            {
                videoElement.Width = 1920;
                videoElement.Height= 1080;

                videoElement.IsEnabled = true;
                videoElement.Visibility = System.Windows.Visibility.Visible;
                
                videoElement.Source = uri;
                videoElement.Stretch = Stretch.Fill;
                videoElement.Play();
            }
        }

        public static void playGif(string path)
        {
            foreach (var videoElement in videoSourcePlayers)
            {
                videoElement.Pause();
                videoElement.IsEnabled = false;
                videoElement.Visibility = System.Windows.Visibility.Hidden;
            }

            Uri uri = new Uri(path);

            foreach (var gifElement in gifAndImageSourcePlayers)
            {
                gifElement.IsEnabled = true;
                gifElement.Visibility = System.Windows.Visibility.Visible;
                gifElement.SetValue(AnimationBehavior.SourceUriProperty, uri);
                gifElement.SetValue(AnimationBehavior.CacheFramesInMemoryProperty, true);
                gifElement.Stretch = Stretch.Fill;
            }
        }

        static void PrintVisibleWindowHandles(IntPtr hwnd, int maxLevel = -1, int level = 0)
        {
            bool isVisible = W32.IsWindowVisible(hwnd);

            if (isVisible && (maxLevel == -1 || level <= maxLevel))
            {
                StringBuilder className = new StringBuilder(256);
                W32.GetClassName(hwnd, className, className.Capacity);

                StringBuilder windowTitle = new StringBuilder(256);
                W32.GetWindowText(hwnd, windowTitle, className.Capacity);


                //Trace.WriteLine("".PadLeft(level * 2) + $"0x{hwnd.ToInt64():X8} \"{windowTitle}\" {className}");

                level++;

                W32.EnumChildWindows(hwnd, new W32.EnumWindowsProc((childhandle, childparamhandle) =>
                {
                    PrintVisibleWindowHandles(childhandle, maxLevel, level);
                    return true;
                }), IntPtr.Zero);
            }
        }
        static void PrintVisibleWindowHandles(int maxLevel = -1)
        {
            W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                PrintVisibleWindowHandles(tophandle, maxLevel);
                return true;
            }), IntPtr.Zero);
        }


        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        public static string GetFocusedWindowTitle()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            const int nChars = 256;
            System.Text.StringBuilder windowTitle = new System.Text.StringBuilder(nChars);
            if (GetWindowText(foregroundWindowHandle, windowTitle, nChars) > 0)
            {
                return windowTitle.ToString();
            }
            return null;
        }

        static bool isWindowFocused()
        {
            string focusedWindowTitle = GetFocusedWindowTitle();
            bool result = !string.IsNullOrEmpty(focusedWindowTitle) ? true : false;
            //Trace.WriteLine(result);
            return result;
        }
    }
}
