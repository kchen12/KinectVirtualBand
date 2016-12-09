//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

// Based off of Vangos kinect-coordinate-mapping

namespace KinectVirtualBand
{
    using System;
    using System.Threading;
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
    using NAudio.Wave;

    class SineWaveOscillator : WaveProvider16
    {
        double phaseAngle;

        public SineWaveOscillator(int sampleRate) :
          base(sampleRate, 1)
        {
        }

        public double Frequency { set; get; }
        public short Amplitude { set; get; }

        public override int Read(short[] buffer, int offset,
          int sampleCount)
        {

            for (int index = 0; index < sampleCount; index++)
            {
                buffer[offset + index] =
                  (short)(Amplitude * Math.Sin(phaseAngle));
                phaseAngle +=
                  2 * Math.PI * Frequency / WaveFormat.SampleRate;

                if (phaseAngle > 2 * Math.PI)
                    phaseAngle -= 2 * Math.PI;
            }
            return sampleCount;
        }
    }

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
            //if (i > 12)
            //{
               // do nothing
            //}
            //else 
            if (noteTriggered != null)
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

        // true = chord mode, false = notesmode
        private bool playMode = true;

        private bool flag1 = false;
        private bool flag2 = false;
        private bool flag3 = false;
        private bool flag4 = false;
        private bool flag5 = false;
        private bool flag6 = false;

        private Point origin = new Point(0,0);
        private float bodZ = 0;

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

        /*
        public void noteHandler()
        {
            while (true)
            {
                MidiListener.noteTriggered += new MIDIEventHandler(playMusic);
            }
        }*/

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
                //lauch threads 
                //Thread handThread = new Thread(noteHandler);
                //handThread.Start();
                //while (!handThread.IsAlive) ;
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
            if (playMode == true)
            {
                Thread handThread = new Thread(() => chordThread(e.GetNote()));
                handThread.Start();
                while (!handThread.IsAlive) ;
            }
            else
            {
                Thread handThread = new Thread(() => noteThread(e.GetNote()));
                handThread.Start();
                while (!handThread.IsAlive) ;
            }
        }

        private void noteThread(int note)
        {
            SineWaveOscillator osc1 = new SineWaveOscillator(44100);

            int note1 = (int)(440.0 * Math.Pow(2.0, ((double)(note + 59) - 69.0) / 12.0));

            osc1.Frequency = note1;

            osc1.Amplitude = 8192;

            WaveOut waveOut1 = new WaveOut();

            waveOut1.Init(osc1);

            waveOut1.Play();

            Thread.Sleep(1000);

            waveOut1.Stop();

            switch (note)
            {
                case 1:
                    flag1 = false;
                    break;
                case 5:
                    flag2 = false;
                    break;
                case 8:
                    flag3 = false;
                    break;
                case 13:
                    flag4 = false;
                    break;
                case 16:
                    flag5 = false;
                    break;
                case 19:
                    flag6 = false;
                    break;
                default:
                    break;
            }
        }

        private void chordThread(int note)
        {
            SineWaveOscillator osc1 = new SineWaveOscillator(44100);
            SineWaveOscillator osc2 = new SineWaveOscillator(44100);
            SineWaveOscillator osc3 = new SineWaveOscillator(44100);

            int note1 = (int)(440.0 * Math.Pow(2.0, ((double)(note+59) - 69.0) / 12.0));
            int note2 = (int)(440.0 * Math.Pow(2.0, ((double)(note + 63) - 69.0) / 12.0));
            int note3 = (int)(440.0 * Math.Pow(2.0, ((double)(note + 66) - 69.0) / 12.0));

            osc1.Frequency = note1;
            osc2.Frequency = note2;
            osc3.Frequency = note3;

            osc1.Amplitude = 8192;
            osc2.Amplitude = 8192;
            osc3.Amplitude = 8192;

            WaveOut waveOut1 = new WaveOut();
            WaveOut waveOut2 = new WaveOut();
            WaveOut waveOut3 = new WaveOut();

            waveOut1.Init(osc1);
            waveOut2.Init(osc2);
            waveOut3.Init(osc3);

            waveOut1.Play();
            waveOut2.Play();
            waveOut3.Play();
            
            Thread.Sleep(1000);

            waveOut1.Stop();
            waveOut2.Stop();
            waveOut3.Stop();

            switch (note)
            {
                case 1:
                    flag1 = false;
                    break;
                case 8:
                    flag2 = false;
                    break;
                case 4:
                    flag3 = false;
                    break;
                case 6:
                    flag4 = false;
                    break;
                case 11:
                    flag5 = false;
                    break;
                case 2:
                    flag6 = false;
                    break;
                default:
                    break;
            }

            /*
            switch (note)
            {
                case 261:
                    flag1 = false;
                    break;
                case 329:
                    flag2 = false;
                    break;
                case 391:
                    flag3 = false;
                    break;
                case 523:
                    flag4 = false;
                    break;
                case 659:
                    flag5 = false;
                    break;
                case 783:
                    flag6 = false;
                    break;
                default:
                    break;
            }*/

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
                            if(Math.Abs(body.Joints[JointType.HandRight].Position.X-body.Joints[JointType.HandLeft].Position.X)<0.01 && Math.Abs(body.Joints[JointType.HandRight].Position.Y - body.Joints[JointType.HandLeft].Position.Y) < 0.01)
                            {
                                if(playMode == true)
                                {
                                    playMode = false;
                                }
                                else
                                {
                                    playMode = true;
                                }
                            }
                            // COORDINATE MAPPING
                            foreach (Joint joint in body.Joints)
                            {
                                // get center of body
                                
                                if(joint.JointType == JointType.HipCenter)
                                {
                                    bodZ = joint.Position.Z;
                                    ColorImagePoint colorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
                                    origin.X = colorPoint.X;
                                    origin.Y = colorPoint.Y;
                                    drawVisual(origin);
                                }
                                
                                // change this to not go through all joints
                                if ((joint.JointType == JointType.HandRight) || (joint.JointType == JointType.HandLeft) || (joint.JointType == JointType.HipCenter))
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

                                    // DRAWING...
                                    Ellipse ellipse = new Ellipse
                                    {
                                        Fill = Brushes.Red,
                                        Width = 20,
                                        Height = 20
                                    };

                                    Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                                    Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                                    canvas.Children.Add(ellipse);

                                    double xoff = origin.X;
                                    double yoff = origin.Y;
                                    // 240 320
                                    
                                    point.Y = -point.Y + yoff;
                                    point.X = point.X - xoff;

                                    System.Drawing.Point polPoint = cartToPol(point.X, point.Y);

                                    if ((joint.JointType == JointType.HandRight || joint.JointType == JointType.HandLeft))
                                    {
                                        float scale = 4f*(bodZ-joint.Position.Z);
                                        scale = 1f;
                                        //Console.WriteLine("hipDist: " + bodZ + " handDist: " + joint.Position.Z + " scale: " + scale);
                                        //Console.WriteLine("scale: " + scale);
                                        if (polPoint.X > 100)
                                        {
                                            if (polPoint.Y >= -30 && polPoint.Y < 10)
                                            {
                                                if (flag1 != true)
                                                {
                                                    flag1 = true;
                                                    int pitch = (int)(60 );
                                                    //int note = (int)(440.0 * Math.Pow(2.0, ((double)(pitch) - 69.0) / 12.0));
                                                    //Console.WriteLine(note);
                                                    // 261
                                                    if (playMode == true)
                                                    {
                                                        MidiListener.add(1);
                                                    }
                                                    else
                                                    {
                                                        MidiListener.add(1);
                                                    }
                                                }
                                            }
                                            else if (polPoint.Y >= 10 && polPoint.Y < 50)
                                            {
                                                if (flag2 != true)
                                                {
                                                    flag2 = true;
                                                    int pitch = (int)(64 );
                                                    //Console.WriteLine(pitch);
                                                    //int note = (int)(440.0 * Math.Pow(2.0, ((double)(pitch) - 69.0) / 12.0));
                                                    //Console.WriteLine(note);
                                                    //329
                                                    if (playMode == true)
                                                    {
                                                        MidiListener.add(8);
                                                    }
                                                    else
                                                    {
                                                        MidiListener.add(5);
                                                    }
                                                }
                                            }
                                            else if (polPoint.Y >= 50 && polPoint.Y < 90)
                                            {
                                                if (flag3 != true)
                                                {
                                                    flag3 = true;
                                                    int pitch = (int)(67);
                                                    //Console.WriteLine(pitch);
                                                    //int note = (int)(440.0 * Math.Pow(2.0, ((double)(pitch) - 69.0) / 12.0));
                                                    //Console.WriteLine(note);
                                                    //391
                                                    if (playMode == true)
                                                    {
                                                        MidiListener.add(4);
                                                    }
                                                    else
                                                    {
                                                        MidiListener.add(8);
                                                    }
                                                }
                                            }
                                            else if (polPoint.Y >= 90 && polPoint.Y < 130)
                                            {
                                                if (flag4 != true)
                                                {
                                                    flag4 = true;
                                                    int pitch = (int)(72 );
                                                    //Console.WriteLine(pitch);
                                                    //int note = (int)(440.0 * Math.Pow(2.0, ((double)(pitch) - 69.0) / 12.0));
                                                    //Console.WriteLine(note);
                                                    //523
                                                    if (playMode == true)
                                                    {
                                                        MidiListener.add(6);
                                                    }
                                                    else
                                                    {
                                                        MidiListener.add(13);
                                                    }
                                                }
                                            }
                                            else if (polPoint.Y >= 130 && polPoint.Y < 170)
                                            {
                                                if (flag5 != true)
                                                {
                                                    flag5 = true;
                                                    int pitch = (int)(76 );
                                                    //Console.WriteLine(pitch);
                                                    //int note = (int)(440.0 * Math.Pow(2.0, ((double)(pitch) - 69.0) / 12.0));
                                                    //Console.WriteLine(note);
                                                    //659
                                                    if (playMode == true)
                                                    {
                                                        MidiListener.add(11);
                                                    }
                                                    else
                                                    {
                                                        MidiListener.add(16);
                                                    }
                                                }
                                            }
                                            else if (polPoint.Y >= 170 && polPoint.Y < 230)
                                            {
                                                if (flag6 != true)
                                                {
                                                    flag6 = true;
                                                    int pitch = (int)(79 );
                                                    //Console.WriteLine(pitch);
                                                    //int note = (int)(440.0 * Math.Pow(2.0, ((double)(pitch) - 69.0) / 12.0));
                                                    //Console.WriteLine(note);
                                                    //783
                                                    if (playMode == true)
                                                    {
                                                        MidiListener.add(2);
                                                    }
                                                    else
                                                    {
                                                        MidiListener.add(19);
                                                    }
                                                }
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

        private void drawVisual(Point origin)
        {
            // check for random joint data
            if(origin.X == 0 && origin.Y == 0)
            {
                return;
            }

            System.Drawing.Point polOrigin = cartToPol(origin.X, origin.Y);
            double lineLength = 250;
            double anglePartition = 40;
            
            for(int i=0; i<7; i++)
            {
                double newAngle = -30 + i * anglePartition;
                newAngle = newAngle * (Math.PI / 180);
                double newX = origin.X + lineLength * Math.Cos(newAngle);
                double newY = origin.Y + -1 * lineLength * Math.Sin(newAngle);
                System.Windows.Point point = polToCart(lineLength, -30+i*anglePartition);
                
                Line line1 = new Line();
                line1.Stroke = Brushes.LightSteelBlue;
                line1.Opacity = 0.5;

                line1.X1 = origin.X;
                line1.X2 = newX;
                line1.Y1 = origin.Y;
                line1.Y2 = newY;
                //Console.WriteLine(origin.X + " " + origin.Y + ", " + newX + " " + newY);

                line1.StrokeThickness = 6;
                canvas.Children.Add(line1);
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
            return new System.Drawing.Point((int)r, (int)((theta * 180) / Math.PI));
        }

        private System.Windows.Point polToCart(double r, double theta)
        {
            theta = theta * (Math.PI / 180);
            double x = r * Math.Cos(theta);
            double y = r * Math.Sin(theta);
            //Console.WriteLine(x + " " + y + ", " + r + " " + theta);
            return new System.Windows.Point(x, y);
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