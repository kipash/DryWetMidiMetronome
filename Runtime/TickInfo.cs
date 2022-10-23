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

        public BPMScaling Scaling;
        /// <summary>
        /// BPM scales based on 
        /// </summary>
        public double ScaledBPM => BPM * (Scaling == BPMScaling.ScaleWithDenumerator ? (TimeSignatureDenumerator / 4d) : 1d);
        public double BeatDuration => 60d / ScaledBPM;

        public static TickInfo Create(ValueChange<Tempo> tempoChange, TempoMap map, BPMScaling scaling) => Create(tempoChange.Time, tempoChange.Value.BeatsPerMinute, map, scaling);
        public static TickInfo Create(long time, double bpm, TempoMap map, BPMScaling scaling)
        {
            var seconds = TimeConverter.ConvertTo<MetricTimeSpan>(time, map).TotalSeconds;
            return Create(seconds, bpm, map, scaling);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time">seconds</param>
        /// <param name="bpm">beats per minute</param>
        public static TickInfo Create(double time, double bpm, TempoMap map, BPMScaling scaling)
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
            info.Scaling = scaling;

            return info;
        }
    }
}