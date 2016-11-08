using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Midi;
using System.Threading;

namespace NAudioTest
{
    class program
    {
        static void Main()
        {
            using (MidiOut midiOut = new MidiOut(0))
            {
                midiOut.Volume = 65535;
                midiOut.Send(MidiMessage.StartNote(60, 127, 1).RawData);
                //MessageBox.Show("Sent");
                Thread.Sleep(300);
                //midiOut.Send(MidiMessage.StopNote(60, 0, 1).RawData);
                Thread.Sleep(300);
            }
        }
    }
}