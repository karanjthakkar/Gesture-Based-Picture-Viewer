//-------------------------------------------------------------------------------
//Copyright (c) 2012, Karan Thakkar
//All rights reserved.

//Redistribution and use in source and binary forms, with or without
//modification, are permitted provided that the following conditions are met: 

//1. Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer. 
//2. Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 

//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
//ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//-------------------------------------------------------------------------------

namespace PictureViewer
{
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.SwipeGestureRecognizer;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Timers;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Sets cursor position
        /// </summary>
        /// <param name="x">X co-ordinate</param>
        /// <param name="y">Y co-ordinate</param>
        /// <returns>Nothing</returns>
        [DllImport("user32")]
        public static extern int SetCursorPos(int x, int y);


        /// <summary>
        /// Virtual Keycodes to simulate mouse events
        /// </summary>
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEVENTF_RIGHTDOWN = 0x008;

        /// <summary>
        /// Simulate mouse event
        /// </summary>
        /// <param name="dwFlags">Used to set value of Left Mouse Button</param>
        /// <param name="dx">Take X co-ordinate</param>
        /// <param name="dy">Take Y co-ordinate</param>
        /// <param name="cButtons">Not used</param>
        /// <param name="dwExtraInfo">Not used</param>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        /// <summary>
        /// Gives the new position of Hand Joint
        /// </summary>
        private Point stop;

        /// <summary>
        /// Changes the maximum value that should be considered while scaling a joint
        /// </summary>
        private const float SkeletonMaxX = 0.40f;
        private const float SkeletonMaxY = 0.3f;
        Point start = new Point(SystemParameters.PrimaryScreenWidth / 2, SystemParameters.PrimaryScreenHeight / 2);

        /// <summary>
        /// Zoom mode on or off (ON = true, OFF = false)
        /// </summary>
        private bool zoom = false;

        /// <summary>
        /// Variables that are passed  to the SetCursorPos() Method
        /// </summary>
        private int topofscreen = 0;
        private int leftofscreen = 0;

        /// <summary>
        /// Used for closing the application after quit button is activated
        /// </summary>
        private int counter = 15;
            
        /// <summary>
        /// Declaring a new cursor
        /// </summary>
        private Cursor newCursor;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine; //---------------------------*Future Use*---------------------------

        // Dispatcher t for other buttons and t2 for quit button and t3 for progressBar of other buttons, t4 for progressBar of quit button. dt for current DateTime
        System.Windows.Threading.DispatcherTimer t, t2, t3, t4, t5, t6;
        DateTime dt;

        /// <summary>
        /// if false, normal operation. if true, quit operation.
        /// </summary>
        private bool quitValue = false;

        //Check the button over which the mouse is.
        Border v = new Border();
        private string s;
        ProgressBar pBar = new ProgressBar();

        /// <summary>
        /// Cursor X and Y position
        /// </summary>
        public int cursorX, cursorY;

        /// <summary>
        /// The recognizer being used.
        /// </summary>
        private readonly Recognizer activeRecognizer;

        /// <summary>
        /// The paths of the picture files.
        /// </summary>
        private string[] picturePaths = CreatePicturePaths("");

        private string[] backPicturePaths = CreateBackgroundPicturePaths();

        /// <summary>
        /// Array of arrays of contiguous line segements that represent a skeleton.
        /// </summary>
        private static readonly JointType[][] SkeletonSegmentRuns = new JointType[][]
        {
            new JointType[] 
            { 
                JointType.Head, JointType.ShoulderCenter, JointType.HipCenter 
            },
            new JointType[] 
            { 
                JointType.HandLeft, JointType.WristLeft, JointType.ElbowLeft, JointType.ShoulderLeft,
                JointType.ShoulderCenter,
                JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight
            },
            new JointType[]
            {
                JointType.FootLeft, JointType.AnkleLeft, JointType.KneeLeft, JointType.HipLeft,
                JointType.HipCenter,
                JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight
            }
        };


        #region Gets metadata for speech recognizer (Future use)
        
        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }
        
        #endregion


        /// <summary>
        /// The sensor we're currently tracking.
        /// </summary>
        private KinectSensor nui;

        /// <summary>
        /// There is currently no connected sensor.
        /// </summary>
        private bool isDisconnectedField = true;

        /// <summary>
        /// Any message associated with a failure to connect.
        /// </summary>
        private string disconnectedReasonField;

        /// <summary>
        /// Array to receive skeletons from sensor, resize when needed.
        /// </summary>
        private Skeleton[] skeletons = new Skeleton[0];

        /// <summary>
        /// Time until skeleton ceases to be highlighted.
        /// </summary>
        private DateTime highlightTime = DateTime.MinValue;

        /// <summary>
        /// The ID of the skeleton to highlight.
        /// </summary>
        private int highlightId = -1;

        /// <summary>
        /// The ID of the skeleton to be tracked.
        /// </summary>
        private int nearestId = -1;

        /// <summary>
        /// The index of the current image.
        /// </summary>
        private int indexField = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            this.Index = 0;
            this.PreviousPicture = this.LoadPicture(this.Index - 1);
            this.Picture = this.LoadPicture(this.Index);
            this.NextPicture = this.LoadPicture(this.Index + 1);
            this.backgroundPic = this.LoadBackgroundPicture(this.backIndex);

            InitializeComponent();

            //For displaying a folders images after some seconds
            t = new System.Windows.Threading.DispatcherTimer();
            t2 = new System.Windows.Threading.DispatcherTimer();
            t3 = new System.Windows.Threading.DispatcherTimer();
            t4 = new System.Windows.Threading.DispatcherTimer();
            t5 = new System.Windows.Threading.DispatcherTimer();
            t6 = new System.Windows.Threading.DispatcherTimer();
            t.Tick += new EventHandler(t_Tick);
            t2.Tick += new EventHandler(t2_tick);
            t3.Tick += new EventHandler(t3_tick);
            t4.Tick += new EventHandler(t4_tick);
            t5.Tick += new EventHandler(t5_tick);
            t6.Tick += new EventHandler(t6_tick);


            //Load custom cursor from resource
            newCursor = new Cursor(new System.IO.MemoryStream(PictureViewer.Resource1.bigArrow));
            window.Cursor = newCursor;

            #region Get the names of folders in the Images Direcotry (Future Use)
            /*
            //Getting folder names in a directory
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string final = path + "\\Images";
            foreach (string dir in Directory.GetDirectories(@final, "*"))
            {
                int i = 2;
                string[] directories = dir.Split(System.IO.Path.DirectorySeparatorChar);
                //newBox.Items.Insert(i, directories[directories.Length-1]);
                i++;
            }
             * */
            #endregion

            //Set the height and width of the Display window
            window.Height = SystemParameters.MaximizedPrimaryScreenHeight;
            window.Width = SystemParameters.MaximizedPrimaryScreenWidth;

            //Create a transform group for Scaling and translating
            TransformGroup group = new TransformGroup();
            ScaleTransform xform = new ScaleTransform();
            group.Children.Add(xform);
            TranslateTransform tt = new TranslateTransform();
            group.Children.Add(tt);
            current.RenderTransform = group;

            // Create the gesture recognizer.
            this.activeRecognizer = this.CreateRecognizer();

            // Wire-up window loaded event.
            Loaded += this.OnMainWindowLoaded;

            //Wire-up window unloaded event
            Unloaded += this.OnMainWindowUnLoaded;

        }

        /// <summary>
        /// Event implementing INotifyPropertyChanged interface.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a value indicating whether no Kinect is currently connected.
        /// </summary>
        public bool IsDisconnected
        {
            get
            {
                return this.isDisconnectedField;
            }

            private set
            {
                if (this.isDisconnectedField != value)
                {
                    this.isDisconnectedField = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("IsDisconnected"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets any message associated with a failure to connect.
        /// </summary>
        public string DisconnectedReason
        {
            get
            {
                return this.disconnectedReasonField;
            }

            private set
            {
                if (this.disconnectedReasonField != value)
                {
                    this.disconnectedReasonField = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("DisconnectedReason"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the index number of the image to be shown.
        /// </summary>
        public int Index
        {
            get
            {
                return this.indexField;
            }

            set
            {
                if (this.indexField != value)
                {
                    this.indexField = value;

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Index"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the index number of the image to be shown
        /// </summary>
        public int backIndex
        {
            get
            {
                return this.indexField;
            }

            set
            {
                if (this.indexField != value)
                {
                    this.indexField = value;

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Index"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the previous image displayed.
        /// </summary>
        public BitmapImage PreviousPicture { get; private set; }

        /// <summary>
        /// Gets the current image to be displayed.
        /// </summary>
        public BitmapImage Picture { get; private set; }

        /// <summary>
        /// Gets the next image displayed.
        /// </summary>
        public BitmapImage NextPicture { get; private set; }

        /// <summary>
        /// Gets the next background iamge to be displayed
        /// </summary>
        public BitmapImage backgroundPic { get; private set; }

        #region Code for creating picture paths
        /// <summary>
        /// Get list of files to display as pictures.
        /// </summary>
        /// <param name="folderName">if empty, entire 'Images' directory is searched. Otherwise, the specified directory is searched</param>
        /// <returns>Path to pictures</returns>
        private static string[] CreatePicturePaths(string folderName)
        {
            var list = new List<string>();
            var commonPicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var newPath = commonPicturesPath + "\\Images\\" + folderName;
            list.AddRange(Directory.GetFiles(newPath, "*.jpg", SearchOption.AllDirectories));
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.png", SearchOption.AllDirectories));

            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.tif", SearchOption.AllDirectories));
            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.gif", SearchOption.AllDirectories));
            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.bmp", SearchOption.AllDirectories));
            }

            return list.ToArray();
        }
        #endregion

        #region Code for Loading picture
        /// <summary>
        /// Load the picture with the given index.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>Corresponding image.</returns>
        private BitmapImage LoadPicture(int index)
        {
            BitmapImage value;

            if (this.picturePaths.Length != 0)
            {
                var actualIndex = index % this.picturePaths.Length;
                if (actualIndex < 0)
                {
                    actualIndex += this.picturePaths.Length;
                }

                Debug.Assert(0 <= actualIndex, "Index used will be non-negative");
                Debug.Assert(actualIndex < this.picturePaths.Length, "Index is within bounds of path array");

                try
                {
                    value = new BitmapImage(new Uri(this.picturePaths[actualIndex]));

                }
                catch (NotSupportedException)
                {
                    value = null;
                }
            }
            else
            {
                value = null;
            }

            return value;
        }
        #endregion

        #region Recognizer for swipe gestures
        /// <summary>
        /// Create a wired-up recognizer for swipe right and swipe left gestures.
        /// </summary>
        /// <returns>The wired-up recognizer for the same gestures</returns>
        private Recognizer CreateRecognizer()
        {
            // Instantiate a recognizer.
            var recognizer = new Recognizer();

            // Wire-up swipe right to manually advance picture -----------------------------------------//
            recognizer.SwipeRightDetected += (s, e) =>
            {

                if (e.Skeleton.TrackingId == nearestId && e.Skeleton.Joints[JointType.HandLeft].Position.Y < e.Skeleton.Joints[JointType.HipCenter].Position.Y
                    && e.Skeleton.Joints[JointType.ElbowLeft].Position.Y > e.Skeleton.Joints[JointType.HipCenter].Position.Y)
                {
                    zoom = false;
                    Index++;

                    // Setup corresponding picture if pictures are available.
                    this.PreviousPicture = this.Picture;
                    this.Picture = this.NextPicture;
                    this.NextPicture = LoadPicture(Index + 1);

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
                    }

                    var storyboard = Resources["LeftAnimate"] as Storyboard;
                    if (storyboard != null)
                    {
                        resetImage();
                        storyboard.Begin();
                    }

                    HighlightSkeleton(e.Skeleton);
                }

            };

            // Wire-up swipe left to manually reverse picture ---------------------------------------------//
            recognizer.SwipeLeftDetected += (s, e) =>
            {
                if (e.Skeleton.TrackingId == nearestId)
                {
                    zoom = false;
                    Index--;
                    // Setup corresponding picture if pictures are available.
                    this.NextPicture = this.Picture;
                    this.Picture = this.PreviousPicture;
                    this.PreviousPicture = LoadPicture(Index - 1);

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
                    }

                    var storyboard = Resources["RightAnimate"] as Storyboard;
                    // resetImage();

                    if (storyboard != null)
                    {
                        resetImage();
                        storyboard.Begin();

                    }

                    HighlightSkeleton(e.Skeleton);

                }
            };

            return recognizer;
        }
        #endregion

        #region Initialize Kinect
        /// <summary>
        /// Handle insertion of Kinect sensor.
        /// </summary>
        private void InitializeNui()
        {
            this.UninitializeNui();

            var index = 0;
            while (this.nui == null && index < KinectSensor.KinectSensors.Count)
            {
                try
                {
                    this.nui = KinectSensor.KinectSensors[index];

                    this.nui.Start();

                    this.IsDisconnected = false;

                    this.DisconnectedReason = null;
                }
                catch (IOException ex)
                {
                    this.nui = null;

                    this.DisconnectedReason = ex.Message;
                }
                catch (InvalidOperationException ex)
                {
                    this.nui = null;

                    this.DisconnectedReason = ex.Message;
                }

                index++;
            }

            if (this.nui != null)
            {
                var parameters = new TransformSmoothParameters
                {
                    Smoothing = 0.3f,
                    Correction = 0.0f,
                    Prediction = 0.0f,
                    JitterRadius = 1.0f,
                    MaxDeviationRadius = 0.5f
                };

                this.nui.SkeletonStream.Enable(parameters);

                this.nui.SkeletonFrameReady += this.OnSkeletonFrameReady;
            }
        }
        #endregion

        #region Uninitialize Kinect after use
        /// <summary>
        /// Handle removal of Kinect sensor.
        /// </summary>
        private void UninitializeNui()
        {
            if (this.nui != null)
            {
                this.nui.SkeletonFrameReady -= this.OnSkeletonFrameReady;

                this.nui.Stop();

                this.nui = null;
            }

            this.IsDisconnected = true;

            this.DisconnectedReason = null;
        }
        #endregion

        #region Event handler for WindowLoaded
        /// <summary>
        /// Window loaded actions to initialize Kinect handling.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {

            // Start the Kinect system, this will cause StatusChanged events to be queued.
            this.InitializeNui();

            // Handle StatusChange events to pick the first sensor that connects.
            KinectSensor.KinectSensors.StatusChanged += (s, ee) =>
            {
                switch (ee.Status)
                {
                    case KinectStatus.Connected:
                        if (nui == null)
                        {
                            Debug.WriteLine("New Kinect connected");

                            InitializeNui();
                        }
                        else
                        {
                            Debug.WriteLine("Existing Kinect signalled connection");
                        }

                        break;
                    default:
                        if (ee.Sensor == nui)
                        {
                            Debug.WriteLine("Existing Kinect disconnected");

                            UninitializeNui();
                        }
                        else
                        {
                            Debug.WriteLine("Other Kinect event occurred");

                            UninitializeNui();
                        }

                        break;
                }
            };

            #region Initializing Speech recognition engine and adding words to the dictionary (Future Use)
            RecognizerInfo ri = GetKinectRecognizer();

            if (null != ri)
            {

                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                var directions = new Choices();
                directions.Add(new SemanticResultValue("click", "CLICK"));
                directions.Add(new SemanticResultValue("clique", "CLICK"));
                directions.Add(new SemanticResultValue("clik", "CLICK"));
                directions.Add(new SemanticResultValue("yes", "YES"));
                directions.Add(new SemanticResultValue("yess", "YES"));
                directions.Add(new SemanticResultValue("yesss", "YES"));
                directions.Add(new SemanticResultValue("nooo", "NO"));
                directions.Add(new SemanticResultValue("no", "NO"));

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);

                var g = new Grammar(gb);

                speechEngine.LoadGrammar(g);

                speechEngine.SpeechRecognized += SpeechRecognized;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;

                speechEngine.SetInputToAudioStream(nui.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            
            #endregion
        }
        #endregion

        #region Event Handler for WindowUnloaded
        private void OnMainWindowUnLoaded(object sender, RoutedEventArgs e)
        {
            this.UninitializeNui();
            System.Environment.Exit(0);
        }
        #endregion

        /// <summary>
        /// Handler for skeleton ready handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                // resize the skeletons array if needed
                if (skeletons.Length != frame.SkeletonArrayLength)
                    skeletons = new Skeleton[frame.SkeletonArrayLength];

                // get the skeleton data
                frame.CopySkeletonDataTo(skeletons);

                foreach (var skeleton in skeletons)
                {
                    // skip the skeleton if it is not being tracked
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        #region Zoom out
                        //Zoom Out
                        // Right and Left Hand in front of Shoulders
                        if (skeleton.Joints[JointType.HandLeft].Position.Z < skeleton.Joints[JointType.ElbowLeft].Position.Z &&
                            skeleton.Joints[JointType.HandRight].Position.Z < skeleton.Joints[JointType.ElbowRight].Position.Z)
                        {
                            //Debug.WriteLine("Zoom 0 - Right hand in front of right shoudler - PASS");

                            // Hands between shoulder and hip
                            if (skeleton.Joints[JointType.HandRight].Position.Y > skeleton.Joints[JointType.HipCenter].Position.Y &&
                                skeleton.Joints[JointType.HandLeft].Position.Y > skeleton.Joints[JointType.HipCenter].Position.Y)
                            {
                                // Hands between shoulders
                                if (skeleton.Joints[JointType.HandRight].Position.X < skeleton.Joints[JointType.ShoulderRight].Position.X &&
                                    skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.ShoulderLeft].Position.X &&
                                    skeleton.Joints[JointType.HandLeft].Position.X > skeleton.Joints[JointType.ShoulderLeft].Position.X &&
                                    skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ShoulderRight].Position.X)
                                {

                                    float distance = Math.Abs(skeleton.Joints[JointType.HandRight].Position.X - skeleton.Joints[JointType.HandLeft].Position.X);
                                    TransformGroup transformGroup = (TransformGroup)current.RenderTransform;
                                    ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];
                                    if (transform.ScaleX != 1)
                                    {
                                        zoom = true;
                                        transform.ScaleX -= 0.02;
                                        transform.ScaleY -= 0.02;
                                    }
                                    else zoom = false;

                                }

                            }

                        }
                        #endregion

                        #region Zoom in
                        //Zoom In
                        // Right and Left Hand in front of Shoulders
                        if (skeleton.Joints[JointType.HandLeft].Position.Z < skeleton.Joints[JointType.ElbowLeft].Position.Z &&
                            skeleton.Joints[JointType.HandRight].Position.Z < skeleton.Joints[JointType.ElbowRight].Position.Z)
                        {
                            // Hands between shoulder and hip
                            if (skeleton.Joints[JointType.HandRight].Position.Y > skeleton.Joints[JointType.HipCenter].Position.Y &&
                                skeleton.Joints[JointType.HandLeft].Position.Y > skeleton.Joints[JointType.HipCenter].Position.Y)
                            {
                                // Hands outside shoulders
                                if (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.ShoulderRight].Position.X &&
                                    skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ShoulderLeft].Position.X &&
                                    Math.Abs(skeleton.Joints[JointType.HandLeft].Position.X - skeleton.Joints[JointType.HandRight].Position.X) < 0.65f)
                                {
                                    zoom = true;
                                    float distance = Math.Abs(skeleton.Joints[JointType.HandRight].Position.X - skeleton.Joints[JointType.HandLeft].Position.X);
                                    TransformGroup transformGroup = (TransformGroup)current.RenderTransform;
                                    ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];
                                    transform.ScaleX += 0.02;
                                    transform.ScaleY += 0.02;
                                }
                            }

                        }
                        #endregion

                        #region Reset Image
                        //Reset Image
                        if (skeleton.Joints[JointType.HandLeft].Position.Y > skeleton.Joints[JointType.ShoulderCenter].Position.Y)
                        {
                            resetImage();
                        }
                        #endregion

                        #region Panning Image or MoveMouse
                        //Pan image OR Move Mouse
                        if (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ShoulderLeft].Position.X &&
                            skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ElbowLeft].Position.X &&
                            Math.Abs(skeleton.Joints[JointType.Spine].Position.X - skeleton.Joints[JointType.ElbowLeft].Position.X) > 0.3f )
                        {
                            if (zoom == true)
                            {
                                var tt = (TranslateTransform)((TransformGroup)current.RenderTransform).Children.First(tr => tr is TranslateTransform);
                                Joint scaledJoint = scaleTo2((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, skeleton.Joints[JointType.HandRight]);
                                stop.X = scaledJoint.Position.X;
                                stop.Y = scaledJoint.Position.Y;
                                Vector v = start - stop;
                                tt.X = (start.X - v.X);
                                tt.Y = (start.Y - v.Y);
                            }
                            else if (zoom == false)
                            {
                                moveMouse(skeleton);
                            }

                        }
                        #endregion
                    }
                }
            }


            // Get the frame. 
            using (var frame = e.OpenSkeletonFrame())
            {
                // Ensure we have a frame.
                if (frame != null)
                {
                    // Resize the skeletons array if a new size (normally only on first call).
                    if (this.skeletons.Length != frame.SkeletonArrayLength)
                    {
                        this.skeletons = new Skeleton[frame.SkeletonArrayLength];
                    }

                    // Get the skeletons.
                    frame.CopySkeletonDataTo(this.skeletons);

                    // Assume no nearest skeleton and that the nearest skeleton is a long way away.
                    var newNearestId = -1;
                    var nearestDistance2 = double.MaxValue;

                    // Look through the skeletons.
                    foreach (var skeleton in this.skeletons)
                    {
                        // Only consider tracked skeletons.
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            // Find the distance squared.
                            var distance2 = (skeleton.Position.X * skeleton.Position.X) +
                                (skeleton.Position.Y * skeleton.Position.Y) +
                                (skeleton.Position.Z * skeleton.Position.Z);

                            // Is the new distance squared closer than the nearest so far?
                            if (distance2 < nearestDistance2)
                            {
                                // Use the new values.
                                newNearestId = skeleton.TrackingId;
                                nearestDistance2 = distance2;
                            }
                        }
                    }

                    if (this.nearestId != newNearestId)
                    {
                        this.nearestId = newNearestId;
                    }

                    // Pass skeletons to recognizer.
                    this.activeRecognizer.Recognize(sender, frame, this.skeletons);

                    this.DrawStickMen(this.skeletons);
                }
            }
        }

        #region Code for Drawing and Highlighting Skeleton
        /// <summary>
        /// Select a skeleton to be highlighted.
        /// </summary>
        /// <param name="skeleton">The skeleton</param>
        private void HighlightSkeleton(Skeleton skeleton)
        {
            // Set the highlight time to be a short time from now.
            this.highlightTime = DateTime.UtcNow + TimeSpan.FromSeconds(0.5);

            // Record the ID of the skeleton.
            this.highlightId = skeleton.TrackingId;
        }

        /// <summary>
        /// Draw stick men for all the tracked skeletons.
        /// </summary>
        /// <param name="skeletons">The skeletons to draw.</param>
        private void DrawStickMen(Skeleton[] skeletons)
        {
            // Remove any previous skeletons.
            StickMen.Children.Clear();

            foreach (var skeleton in skeletons)
            {
                // Only draw tracked skeletons.
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    // Draw a background for the next pass.
                    this.DrawStickMan(skeleton, Brushes.WhiteSmoke, 7);
                }
            }

            foreach (var skeleton in skeletons)
            {
                // Only draw tracked skeletons.
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    // Pick a brush, Red for a skeleton that has recently gestures, white for the nearest, WhiteSmoke otherwise.
                    var brush = DateTime.UtcNow < this.highlightTime && skeleton.TrackingId == this.highlightId ? Brushes.Red :
                        skeleton.TrackingId == this.nearestId ? Brushes.White : Brushes.WhiteSmoke;

                    // Draw the individual skeleton.
                    this.DrawStickMan(skeleton, brush, 3);
                }
            }
        }

        /// <summary>
        /// Draw an individual skeleton.
        /// </summary>
        /// <param name="skeleton">The skeleton to draw.</param>
        /// <param name="brush">The brush to use.</param>
        /// <param name="thickness">This thickness of the stroke.</param>
        private void DrawStickMan(Skeleton skeleton, Brush brush, int thickness)
        {
            foreach (var run in SkeletonSegmentRuns)
            {
                var next = this.GetJointPoint(skeleton, run[0]);
                for (var i = 1; i < run.Length; i++)
                {
                    var prev = next;
                    next = this.GetJointPoint(skeleton, run[i]);

                    var line = new Line
                    {
                        Stroke = brush,
                        StrokeThickness = thickness,
                        X1 = prev.X,
                        Y1 = prev.Y,
                        X2 = next.X,
                        Y2 = next.Y,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round
                    };

                    StickMen.Children.Add(line);
                }
            }
        }

        /// <summary>
        /// Convert skeleton joint to a point on the StickMen canvas.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="jointType">The joint to project.</param>
        /// <returns>The projected point.</returns>
        private Point GetJointPoint(Skeleton skeleton, JointType jointType)
        {
            var joint = skeleton.Joints[jointType];

            // Points are centered on the StickMen canvas and scaled according to its height allowing
            // approximately +/- 1.5m from center line.
            var point = new Point
            {
                X = (StickMen.Width / 2) + (StickMen.Height * joint.Position.X / 3),
                Y = (StickMen.Width / 2) - (StickMen.Height * joint.Position.Y / 3)
            };

            return point;
        }
        #endregion

        #region Code for Reset Image
        /// <summary>
        /// Reset picture zoom and pan
        /// </summary>
        public void resetImage()
        {
            var st = (ScaleTransform)((TransformGroup)current.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var tt = (TranslateTransform)((TransformGroup)current.RenderTransform).Children.First(tr => tr is TranslateTransform);
            st.ScaleX = 1.0;
            st.ScaleY = 1.0;
            tt.X = 0;
            tt.Y = 0;
            zoom = false;
        }
        #endregion

        #region Code for Scaling function 1 & 2
        /// <summary>
        /// Returns the scaled value after a call to the Scale1 function
        /// </summary>
        /// <param name="width">Width of scrren</param>
        /// <param name="height">Height of screen</param>
        /// <param name="joint">Name of joint to be tracked</param>
        /// <returns></returns>
        public static Joint scaleTo1(int width, int height, Joint joint)
        {
            Microsoft.Kinect.SkeletonPoint pos = new SkeletonPoint()
            {
                X = Scale1(width, joint.Position.X),
                Y = Scale1(height, -joint.Position.Y),
                Z = joint.Position.Z
            };

            pos.Y += pos.Y + (float)(1.1*height);
            pos.X += pos.X - width / 2;
            joint.Position = pos;

            return joint;
        }


        /// <summary>
        /// Returns the scaled value after a call to the Scale1 function (for panning)
        /// </summary>
        /// <param name="width">Width of scrren</param>
        /// <param name="height">Height of screen</param>
        /// <param name="joint">Name of joint to be tracked</param>
        /// <returns></returns>
        public static Joint scaleTo2(int width, int height, Joint joint)
        {
            Microsoft.Kinect.SkeletonPoint pos = new SkeletonPoint()
            {
                X = Scale1(width, joint.Position.X),
                Y = Scale1(height, -joint.Position.Y),
                Z = joint.Position.Z
            };

            joint.Position = pos;

            return joint;
        }



        /// <summary>
        /// Function for translating skelton coordinates to screen position
        /// </summary>
        /// <param name="maxPixel">Screen resolution parameters</param>
        /// <param name="position">Join position (X/Y) </param>
        /// <returns></returns>
        public static float Scale1(int maxPixel, float position)
        {
            float value = ((maxPixel * position));
            return value;
        }

        #endregion

        #region Can be used to display MessabeBox after Quit gesture is recognized (Future Use)
        /*
                private void quit()
                {
                    MessageBoxButton button = MessageBoxButton.YesNo;
                    MessageBoxImage image = MessageBoxImage.Warning;
                    MessageBoxResult result = MessageBox.Show("Are you sure you want to quit?", "Close Picture Viewer :(", button, image);
                    quitValue = true;
                    if (result == MessageBoxResult.Yes) window.Close();
                    else if (result == MessageBoxResult.No) pause = false;
                    else pause = false;
                }
        */
        #endregion

        #region Code for moving mouse
        private void moveMouse(Skeleton sd)
        {
            if (sd.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked && sd.Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked)
            {
                Microsoft.Kinect.SkeletonPoint oldPos = new Microsoft.Kinect.SkeletonPoint();
                Joint scaledRight = scaleTo1((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, sd.Joints[JointType.HandRight]);
                oldPos.X = scaledRight.Position.X;
                oldPos.Y = scaledRight.Position.Y;
                oldPos.Z = scaledRight.Position.Z;

                leftofscreen = Convert.ToInt32(oldPos.X);
                topofscreen = Convert.ToInt32(oldPos.Y);

                SetCursorPos(leftofscreen, topofscreen);
            }
        }
        #endregion

        #region Handler for Speech Recognized & Speech Rejected events (Future Use)
        
        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "CLICK":
                        mouse_event(MOUSEEVENTF_LEFTDOWN, leftofscreen, topofscreen, 0, 0);
                        System.Threading.Thread.Sleep(10);
                        mouse_event(MOUSEEVENTF_LEFTUP, leftofscreen, topofscreen, 0, 0);
                        break;
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            return;
        }
        
        #endregion

        #region Link to website
        //Hyperlink to website
        private void onLinkClick(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        
        #endregion

        #region Code for changing to different folders (Future use)
        /*private void OnButtonClickGo(object sender, RoutedEventArgs e)
        {
            Index = 0;
            string temp = (string)(newBox.SelectedItem);
            if (temp == "All" || newBox.SelectedIndex == 0)
            {
                picturePaths = CreatePicturePaths("");
            }
            else picturePaths = CreatePicturePaths(temp);
            this.PreviousPicture = this.LoadPicture(this.Index - 1);
            this.Picture = this.LoadPicture(this.Index);
            this.NextPicture = this.LoadPicture(this.Index + 1);
            // Notify world of change to Index and Picture.
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
            }
            return;
        }*/
        #endregion

        #region Code for delay function (Future Use)
        /*
        public static DateTime delay(int MilliSecondsToPauseFor)
        {


            System.DateTime ThisMoment = System.DateTime.Now;
            System.TimeSpan duration = new System.TimeSpan(0, 0, 0, 0, MilliSecondsToPauseFor);
            System.DateTime AfterWards = ThisMoment.Add(duration);


            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = System.DateTime.Now;
            }


            return System.DateTime.Now;
        }*/
        #endregion

        #region Code for triggering `folder change` event if mouse stays on button for 2 seconds (5 seconds for Quit button)
        private void onMouseEnter(object sender, MouseEventArgs e)
        {
            if (!quitValue)
            {
                this.Cursor = Cursors.Hand;
                dt = DateTime.Now;
                v = (Border)sender;
                s = v.Name;
                switch (s)
                {
                    case "AllImages":
                        pBar = (ProgressBar)allBar;
                        break;
                    case "Cars":
                        pBar = (ProgressBar)carsBar;
                        break;
                    case "Jewellery":
                        pBar = (ProgressBar)jewelleryBar;
                        break;
                    case "Kitchen":
                        pBar = (ProgressBar)kitchenBar;
                        break;
                    case "Dresses":
                        pBar = (ProgressBar)dressesBar;
                        break;
                    case "back":
                        pBar = (ProgressBar)backBar;
                        this.backIndex++;
                        break;
                }
                pBar.Visibility = System.Windows.Visibility.Visible;
                t.Interval = new TimeSpan(0, 0, 1);
                t.IsEnabled = true;
                t3.Interval = new TimeSpan(0, 0, 0, 0, 32);
                t3.IsEnabled = true;
            }
        }

        private void onMouseLeave(object sender, MouseEventArgs e)
        {
            if (!quitValue)
            {
                this.Cursor = newCursor;
                pBar.Visibility = System.Windows.Visibility.Collapsed;
                pBar.Value = 0;
                switch (s)
                {
                    case "AllImages":
                        allLabel.Foreground = Brushes.Black;
                        break;
                    case "Cars":
                        carsLabel.Foreground = Brushes.Black;
                        break;
                    case "Jewellery":
                        jewelleryLabel.Foreground = Brushes.Black;
                        break;
                    case "Kitchen":
                        kitchenLabel.Foreground = Brushes.Black;
                        break;
                    case "Dresses":
                        dressesLabel.Foreground = Brushes.Black;
                        break;
                    case "back":
                        backLabel.Foreground = Brushes.Black;
                        break;
                }
                t.IsEnabled = false;
                t3.IsEnabled = false;
            }
        }

        void t_Tick(object sender, EventArgs e)
        {

            if ((DateTime.Now - dt).Seconds >= 2)
            {
                switch (s)
                {
                    case "AllImages":
                        picturePaths = CreatePicturePaths("");
                        break;
                    case "Cars":
                        picturePaths = CreatePicturePaths("Cars");
                        break;
                    case "Jewellery":
                        picturePaths = CreatePicturePaths("Jewellery");
                        break;
                    case "Kitchen":
                        picturePaths = CreatePicturePaths("Kitchen");
                        break;
                    case "Dresses":
                        picturePaths = CreatePicturePaths("Dresses");
                        break;
                    case "back":
                        backPicturePaths = CreateBackgroundPicturePaths();
                        break;
                }

                if (s != "back")
                {
                    Index = 0;
                    this.PreviousPicture = this.LoadPicture(this.Index - 1);
                    this.Picture = this.LoadPicture(this.Index);
                    this.NextPicture = this.LoadPicture(this.Index + 1);

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
                    }
                }
                else if (s == "back")
                {
                    this.backgroundPic = this.LoadBackgroundPicture(this.backIndex);
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("backgroundPic"));
                    }
                }
                pBar.Visibility = System.Windows.Visibility.Collapsed;
                pBar.Value = 0;
                switch (s)
                {
                    case "AllImages":
                        pBar = (ProgressBar)allBar;
                        allLabel.Foreground = Brushes.White;
                        break;
                    case "Cars":
                        pBar = (ProgressBar)carsBar;
                        carsLabel.Foreground = Brushes.White;
                        break;
                    case "Jewellery":
                        pBar = (ProgressBar)jewelleryBar;
                        jewelleryLabel.Foreground = Brushes.White;
                        break;
                    case "Kitchen":
                        pBar = (ProgressBar)kitchenBar;
                        kitchenLabel.Foreground = Brushes.White;
                        break;
                    case "Dresses":
                        pBar = (ProgressBar)dressesBar;
                        dressesLabel.Foreground = Brushes.White;
                        break;
                    case "back":
                        pBar = (ProgressBar)backBar;
                        backLabel.Foreground = Brushes.White;
                        break;

                }
            
            }

        }


        private void onMouseEnterQ(object sender, MouseEventArgs e)
        {
            if (!quitValue)
            {
                this.Cursor = Cursors.Hand;
                dt = DateTime.Now;
                pBar = (ProgressBar)quitBar;
                t2.Interval = new TimeSpan(0, 0, 1);
                t2.IsEnabled = true;
                t4.Interval = new TimeSpan(0, 0, 0, 0, 94);
                t4.IsEnabled = true;
                pBar.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void onMouseLeaveQ(object sender, MouseEventArgs e)
        {
            this.Cursor = newCursor;
            if (!quitValue)
            {
                t2.IsEnabled = false;
                t4.IsEnabled = false;
                pBar.Visibility = System.Windows.Visibility.Collapsed;
                pBar.Value = 0;
            }
        }

        private void t2_tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - dt).Seconds >= 5)
            {
                quitValue = true;
                this.backgroundPic = this.LoadBackgroundPicture(0);
                this.PropertyChanged(this, new PropertyChangedEventArgs("backgroundPic"));
                window.Text2.Text = null;
                window.back.Opacity = 0;
                window.Quit.Opacity = 0;
                window.Dresses.Opacity = 0;
                window.Cars.Opacity = 0;
                window.Kitchen.Opacity = 0;
                window.Jewellery.Opacity = 0;
                window.current.Opacity = 0;
                window.AllImages.Opacity = 0;
                window.allBar.Visibility = Visibility.Hidden;
                window.quitBar.Visibility = Visibility.Hidden;
                window.dressesBar.Visibility = Visibility.Hidden;
                window.carsBar.Visibility = Visibility.Hidden;
                window.kitchenBar.Visibility = Visibility.Hidden;
                window.jewelleryBar.Visibility = Visibility.Hidden;
                window.backBar.Visibility = Visibility.Hidden;
                window.Text3.Opacity = 1;
                window.Text4.Opacity = 1;
                window.Text5.Opacity = 1;
                window.Text6.Opacity = 1;
                window.Text7.Opacity = 1;
                window.Text8.Opacity = 1;
                window.Text9.Opacity = 1;
                window.Text2_Border.Opacity = 0;
                t5.Interval = new TimeSpan(0, 0, 0, 0, 1000);
                t5.IsEnabled = true;
                t.IsEnabled = false;
                t2.IsEnabled = false;
                t3.IsEnabled = false;
                t4.IsEnabled = false;
            }
        }

        private void t3_tick(object sender, EventArgs e)
        {
            pBar.Value += 2;
        }

        private void t4_tick(object sender, EventArgs e)
        {
            pBar.Value += 2;
        }

        private void t5_tick(object sender, EventArgs e)
        {

            window.Text8.Text = " " + counter + " seconds.";
            counter--;
            if (counter == -2)
                t6_tick(sender, e);
        }

        private void t6_tick(object sender, EventArgs e)
        {
            window.Close();
        }

        #endregion

        #region Create Background Pictures path. Load Background pictures
        
        /// <summary>
        /// Get list of files to display as pictures.
        /// </summary>
        /// <returns>Path to pictures</returns>
        private static string[] CreateBackgroundPicturePaths()
        {
            var list = new List<string>();
            var commonPicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var newPath = commonPicturesPath + "\\Background";
            list.AddRange(Directory.GetFiles(newPath, "*.jpg", SearchOption.AllDirectories));
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.png", SearchOption.AllDirectories));

            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.tif", SearchOption.AllDirectories));
            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.gif", SearchOption.AllDirectories));
            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.bmp", SearchOption.AllDirectories));
            }

            return list.ToArray();
        }
        
        /// <summary>
        /// Load the picture with the given index.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>Corresponding image.</returns>
        private BitmapImage LoadBackgroundPicture(int index)
        {
            BitmapImage value;

            if (this.backPicturePaths.Length != 0)
            {
                var actualIndex = index % this.backPicturePaths.Length;
                if (actualIndex < 0)
                {
                    actualIndex += this.backPicturePaths.Length;
                }

                Debug.Assert(0 <= actualIndex, "Index used will be non-negative");
                Debug.Assert(actualIndex < this.backPicturePaths.Length, "Index is within bounds of path array");

                try
                {
                    value = new BitmapImage(new Uri(this.backPicturePaths[actualIndex]));

                }
                catch (NotSupportedException)
                {
                    value = null;
                }
            }
            else
            {
                value = null;
            }

            return value;
        }
        
        #endregion
    }
}