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


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int handle = 0;
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

            MidiOutCaps myCaps = new MidiOutCaps();
            var res = MIDI.midiOutGetDevCaps(0, ref myCaps,
               (UInt32)Marshal.SizeOf(myCaps));

            res = MIDI.midiOutOpen(ref handle, 0, null, 0, 0);

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
            res = MIDI.midiOutClose(handle);
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

                                    /*if((joint.JointType == JointType.HandRight) && (point.X > 100))
                                    {
                                        for (int i = 0; i < 1000; i++)
                                        {
                                            MIDI.midiOutShortMsg(handle, 0x007F1990);
                                            MIDI.midiOutShortMsg(handle, 0x007F4A90);
                                            MIDI.midiOutShortMsg(handle, 0x007F1990);
                                            MIDI.midiOutShortMsg(handle, 0x007F4A90);
                                            MIDI.midiOutShortMsg(handle, 0x007F1990);
                                            MIDI.midiOutShortMsg(handle, 0x007F4A90);
                                            MIDI.midiOutShortMsg(handle, 0x007F1990);
                                            MIDI.midiOutShortMsg(handle, 0x007F4A90);
                                        }
                                    }*/
                                    //res = MIDI.midiOutShortMsg(handle, 0x007F1990);
                                }
                            }
                        }
                    }
                }
            }
            
        }

        public void DrawPoint(ColorImagePoint point)
        {
            // Create an ellipse.
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.Red
            };

            // Position the ellipse according to the point's coordinates.
            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

            // Add the ellipse to the canvas.
            canvas.Children.Add(ellipse);
        }
    }
}