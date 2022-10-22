namespace MidiMetronome
{
    public class MetronomeInfo
    {
        /// <summary>
        /// Pregenerated metronome beats with their context: BPM, Time signature
        /// </summary>
        public TickInfo[] Beats;

        /// <summary>
        /// Used for debuging. Contains Tempo and Time signature changes.
        /// </summary>
        public TickInfo[] Changes;
    }
}