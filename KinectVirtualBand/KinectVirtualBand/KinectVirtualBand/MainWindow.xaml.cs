//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

// Based off of Vangos kinect-coordinate-mapping

namespace KinectVirtualBand
{
    using System;
    //using System.Drawing;
    using System.Runtime.InteropServices;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Windows.Controls;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using NAudio.Midi;

    // create event handler for playing MIDI
    public delegate void MIDIEventHandler(object sender, MIDIEventArgs e);

    public class MIDIEventArgs : EventArgs
    {
        private int _note;
        public MIDIEventArgs(int note)
        {
            _note = note;
        }
        public int GetNote()
        {
            return _note;
        }
    }

    public class MIDIListener
    {
        public event MIDIEventHandler noteTriggered;
        
        public void add(int i)
        {
            if (i > 12)
            {
                // do nothing
            }
            else if (noteTriggered != null)
            {
                noteTriggered(this, new MIDIEventArgs(i));
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MIDIListener MidiListener = new MIDIListener();
        
        private bool isPlaying = false;
        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the color data recieved from the camera
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        Skeleton[] _bodies = new Skeleton[6];

        CameraMode _mode = CameraMode.Color;

        enum CameraMode
        {
            Color,
            Depth
        }

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {

                MidiListener.noteTriggered += new MIDIEventHandler(playMusic);

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();
                
                //this.sensor.DepthStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                //this.sensor.AllFramesReady += delegate(object _sender, AllFramesReadyEventArgs _e) { this.SensorAllFramesReady(sender, e)};
                this.sensor.AllFramesReady += this.SensorAllFramesReady;
                
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                //this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void playMusic(object sender, MIDIEventArgs e)
        {
            if (isPlaying != true)
            {
                using (MidiOut midiOut = new MidiOut(0))
                {
                    //isPlaying = true;
                    midiOut.Volume = 65535;
                    midiOut.Send(MidiMessage.StartNote((60+e.GetNote()), 127, 1).RawData);
                    System.Threading.Thread.Sleep(400);
                    midiOut.Send(MidiMessage.StopNote(60+e.GetNote(), 0, 1).RawData);
                    System.Threading.Thread.Sleep(400);
                    //isPlaying = false;
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            

            // Color
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Color)
                    {
                        // Copy the pixel data from the image to a temporary array
                        frame.CopyPixelDataTo(this.colorPixels);

                        // Write the pixel data into our bitmap
                        this.colorBitmap.WritePixels(
                            new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                            this.colorPixels,
                            this.colorBitmap.PixelWidth * sizeof(int),
                            0);
                        camera.Source = colorBitmap;
                    }
                }
            }

            // Depth
            /*using (var frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Depth)
                    {
                        // Copy the pixel data from the image to a temporary array
                        frame.CopyPixelDataTo(this.colorPixels);

                        // Write the pixel data into our bitmap
                        this.colorBitmap.WritePixels(
                            new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                            this.colorPixels,
                            this.colorBitmap.PixelWidth * sizeof(int),
                            0);
                        camera.Source = colorBitmap;
                    }
                }
            }*/

            // Body
            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    frame.CopySkeletonDataTo(_bodies);

                    foreach (var body in _bodies)
                    {
                        if (body.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            // COORDINATE MAPPING
                            foreach (Joint joint in body.Joints)
                            {
                                // change this to not go through all joints
                                if ((joint.JointType == JointType.HandRight) || (joint.JointType == JointType.HandLeft))
                                {
                                    // 3D coordinates in meters
                                    SkeletonPoint skeletonPoint = joint.Position;

                                    // 2D coordinates in pixels
                                    Point point = new Point();

                                    if (_mode == CameraMode.Color)
                                    {
                                        // Skeleton-to-Color mapping
                                        ColorImagePoint colorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint, ColorImageFormat.RgbResolution640x480Fps30);

                                        point.X = colorPoint.X;
                                        point.Y = colorPoint.Y;
                                    }
                                    /*else if (_mode == CameraMode.Depth) // Remember to change the Image and Canvas size to 320x240.
                                    {
                                        // Skeleton-to-Depth mapping
                                        DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skeletonPoint, DepthImageFormat.Resolution320x240Fps30);

                                        point.X = depthPoint.X;
                                        point.Y = depthPoint.Y;
                                    }*/

                                    // DRAWING...
                                    Ellipse ellipse = new Ellipse
                                    {
                                        Fill = Brushes.LightBlue,
                                        Width = 20,
                                        Height = 20
                                    };

                                    Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                                    Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                                    canvas.Children.Add(ellipse);

                                    point.Y = -point.Y+240;
                                    point.X = point.X - 320;

                                    System.Drawing.Point polPoint = cartToPol(point.X, point.Y);

                                    if(joint.JointType == JointType.HandLeft)
                                    {
                                        Console.WriteLine(polPoint.X + " " + polPoint.Y);
                                    }

                                    if((joint.JointType == JointType.HandRight || joint.JointType == JointType.HandLeft))
                                    {
                                        if (polPoint.X > 200)
                                        {
                                            if(polPoint.Y >=-30 && polPoint.Y < 0)
                                            {
                                                MidiListener.add(0);
                                            }
                                            else if (polPoint.Y >= 0 && polPoint.Y < 30)
                                            {
                                                MidiListener.add(2);
                                            }
                                            else if (polPoint.Y >= 30 && polPoint.Y < 60)
                                            {
                                                MidiListener.add(4);
                                            }
                                            else if (polPoint.Y >= 60 && polPoint.Y < 90)
                                            {
                                                MidiListener.add(5);
                                            }
                                            else if (polPoint.Y >= 90 && polPoint.Y < 120)
                                            {
                                                MidiListener.add(7);
                                            }
                                            else if (polPoint.Y >= 120 && polPoint.Y < 150)
                                            {
                                                MidiListener.add(9);
                                            }
                                            else if (polPoint.Y >= 150 && polPoint.Y <= 180)
                                            {
                                                MidiListener.add(11);
                                            }
                                            else if (polPoint.Y > -180 && polPoint.Y < -150)
                                            {
                                                MidiListener.add(12);
                                            }
                                            else
                                            {
                                                // do nothing
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
        }

        /// <summary>
        /// takes cartesian points x and y and returns a point
        /// note the "Point.x" is radius and "Point.Y" is theta
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private System.Drawing.Point cartToPol(double x, double y)
        {
            double r = Math.Sqrt((x * x) + (y * y));
            double theta = Math.Atan2(y, x);
            return new System.Drawing.Point((int)r,(int)((theta*180)/Math.PI));
        }

        public void DrawPoint(ColorImagePoint point)
        {
            // Create an ellipse.
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = System.Windows.Media.Brushes.Red
            };

            // Position the ellipse according to the point's coordinates.
            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

            // Add the ellipse to the canvas.
            canvas.Children.Add(ellipse);
        }
    }
}