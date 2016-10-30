using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MIDITest
{

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
            int handle = 0;

            var numDevs = midiOutGetNumDevs();
            Console.WriteLine("You have {0} midi output devices", numDevs);
            //Console.WriteLine(numDevs);
            MidiOutCaps myCaps = new MidiOutCaps();
            var res = midiOutGetDevCaps(0, ref myCaps,
               (UInt32)Marshal.SizeOf(myCaps));

            //sMciMidiTest();

            res = midiOutOpen(ref handle, 0, null, 0, 0);
            Console.WriteLine(res);
            
            // Figure out why it takes 10000 loops to play a note
            // figure out how to get how long the note is played
            for (int i = 0; i < 10000; i++)
            {
                res = midiOutShortMsg(handle, 0x007F1990);
                res = midiOutShortMsg(handle, 0x007F4A90);
            }
            
            res = midiOutClose(handle);

            Console.ReadLine();

        }

    }

}