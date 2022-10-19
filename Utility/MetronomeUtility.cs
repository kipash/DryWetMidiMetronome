using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MidiMetronome
{
    public static class MetronomeUtility
    {
        public static TickInfo[] GenerateBeats(string path) => GenerateBeats(MidiFile.Read(path));
        public static TickInfo[] GenerateBeats(byte[] raw)
        {
            using (MemoryStream ms = new MemoryStream(raw))
                return GenerateBeats(MidiFile.Read(ms));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="midi"></param>
        /// <param name="stepPrecision">Sampling interval in seconds</param>
        /// <returns></returns>
        public static TickInfo[] GenerateBeats(MidiFile midi)
        {
            //Tempo map contains all tempo and duration information (sort of a timeline)
            var tempoMap = midi.GetTempoMap();

            //Total duration of the midi file
            TimeSpan duration = midi.GetDuration<MetricTimeSpan>();

            //Buffer to contain all 
            TickInfo[] changes = null;

            var rawChanges = tempoMap.GetTempoChanges();
            changes = rawChanges.Select(x => new TickInfo().From(x, tempoMap))
                                .ToArray();

            if (changes == null)
            {
                var initialTempo = tempoMap.GetTempoAtTime(new MetricTimeSpan());
                changes = new TickInfo[] {
                    new TickInfo() { BPM = initialTempo.BeatsPerMinute }
                };
            }

            List<TickInfo> ticks = new();

            for (int i = 0; i < changes.Length; i++)
            {
                bool isLast = (i + 1) >= changes.Length;
                bool isFirst = i == 0;


                TickInfo current = changes[i];

                TickInfo next;
                if (isLast)
                {
                    next.BPM = current.BPM;
                    next.Time = duration.TotalSeconds;
                }
                else
                    next = changes[i + 1];

                //  How far is the metronome in its cycle
                double progress = next.Time % current.BPM_Seconds;

                // [First tick]
                if(isFirst)
                {
                    TickInfo firstTick = new()
                    {
                        Time = 0,
                        BPM = current.BPM
                    };
                    ticks.Add(firstTick);
                }

                // [All full ticks] - fit as many full beats inside the duration between current and next
                double fullStep = current.BPM_Seconds;

                double fullSegmentDuration = next.Time - current.Time;
                int fullSegmentTicksCount = (int)(fullSegmentDuration / current.BPM);
                for (int seg = 1; seg <= fullSegmentTicksCount; seg++)
                {
                    TickInfo currentTick = new()
                    {
                        Time = current.Time + current.BPM_Seconds * seg,
                        BPM = current.BPM
                    };
                    ticks.Add(currentTick);
                }

                // Inset the first tick of the new tempo
                TickInfo nextSegmentTick = new()
                {
                    Time = next.Time,
                    BPM = next.BPM
                };
                ticks.Add(nextSegmentTick);
            }

            return ticks.ToArray();
        }
    }

    public struct TickInfo
    {
        public double Time;
        public double BPM;
        public double BPM_Seconds => BPM / 60d;

        public TickInfo From(ValueChange<Tempo> valueChange, TempoMap map)
        {
            var seconds = TimeConverter.ConvertTo<MetricTimeSpan>(valueChange.Time, map);

            Time = seconds.TotalSeconds;
            BPM = valueChange.Value.BeatsPerMinute;

            return this;
        }
    }
}