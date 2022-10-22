using Melanchall.DryWetMidi.Interaction;

namespace MidiMetronome
{
    public struct TickInfo
    {
        public double Time;
        public double BPM;
        public double TimeSignatureNumber;
        public double TimeSignatureDenumerator;

        public bool IsMeasureBeat;

        /// <summary>
        /// BPM scales based on 
        /// </summary>
        public double ScaledBPM => BPM * (TimeSignatureDenumerator / 4d);
        public double BeatDuration => 60d / ScaledBPM;

        public static TickInfo Create(ValueChange<Tempo> tempoChange, TempoMap map) => Create(tempoChange.Time, tempoChange.Value.BeatsPerMinute, map);
        public static TickInfo Create(long time, double bpm, TempoMap map)
        {
            var seconds = TimeConverter.ConvertTo<MetricTimeSpan>(time, map).TotalSeconds;
            return Create(seconds, bpm, map);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time">seconds</param>
        /// <param name="bpm">beats per minute</param>
        public static TickInfo Create(double time, double bpm, TempoMap map)
        {
            var info = new TickInfo();
            var span = new MetricTimeSpan((long)(time * 1_000_000));
            var signature = map.GetTimeSignatureAtTime(span);

            info.TimeSignatureNumber = signature.Numerator;
            info.TimeSignatureDenumerator = signature.Denominator;

            info.Time = time;
            //if (bpm > 550)
            //    Debug.LogError("Why?");
            info.BPM = bpm;

            return info;
        }
    }
}