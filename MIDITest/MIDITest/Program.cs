using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NAudio.Wave;

namespace MIDITest
{
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

    class program
    {
        static void Main()
        {
            SineWaveOscillator osc = new SineWaveOscillator(44100);
            osc.Frequency = 440;
            osc.Amplitude = 8192;

            WaveOut waveOut = new WaveOut();
            waveOut.Init(osc);
            waveOut.Play();
            //Thread.Sleep(1000);
            //waveOut.Stop();
        }
    }
    /*
    [StructLayout(LayoutKind.Sequential)]
    public struct MidiOutCaps
    {
        public UInt16 wMid;
        public UInt16 wPid;
        public UInt32 vDriverVersion;

        [MarshalAs(UnmanagedType.ByValTStr,
           SizeConst = 32)]
        public String szPname;

        public UInt16 wTechnology;
        public UInt16 wVoices;
        public UInt16 wNotes;
        public UInt16 wChannelMask;
        public UInt32 dwSupport;
    }

    class Program
    {
        // MCI INterface
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string command,
           StringBuilder returnValue, int returnLength,
           IntPtr winHandle);

        // Midi API
        [DllImport("winmm.dll")]
        private static extern int midiOutGetNumDevs();

        [DllImport("winmm.dll")]
        private static extern int midiOutGetDevCaps(Int32 uDeviceID,
           ref MidiOutCaps lpMidiOutCaps, UInt32 cbMidiOutCaps);

        [DllImport("winmm.dll")]
        private static extern int midiOutOpen(ref int handle,
           int deviceID, MidiCallBack proc, int instance, int flags);

        [DllImport("winmm.dll")]
        private static extern int midiOutShortMsg(int handle,
           int message);

        [DllImport("winmm.dll")]
        private static extern int midiOutClose(int handle);

        private delegate void MidiCallBack(int handle, int msg,
           int instance, int param1, int param2);

        static string Mci(string command)
        {
            StringBuilder reply = new StringBuilder(256);
            mciSendString(command, reply, 256, IntPtr.Zero);
            return reply.ToString();
        }

        static void MciMidiTest()
        {
            var res = String.Empty;

            res = Mci("open C:\\Users\\kchen12\\Desktop\\Fall2016\\CSE379\\KinectVirtualBand\\MIDITest\\MIDITest\\MIDI_sample.mid alias music");
            res = Mci("play music");
            Console.ReadLine();
            res = Mci("close crooner");
        }

        static void Main()
        {
            using (MidiOut midiOut = new MidiOut(0))
            {
                midiOut.Volume = 65535;
                midiOut.Send(MidiMessage.StartNote(100, 127, 0).RawData);
                //MessageBox.Show("Sent");
                Thread.Sleep(1000);
                midiOut.Send(MidiMessage.StopNote(100, 0, 0).RawData);
                Thread.Sleep(1000);
            }

        }

    }*/

}