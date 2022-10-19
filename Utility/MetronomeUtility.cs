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

        public static TickInfo[] GenerateBeats(MidiFile midi)
        {
            //Tempo map contains all tempo and duration information (sort of a timeline)
            var tempoMap = midi.GetTempoMap();

            //Total duration of the midi file
            TimeSpan duration = midi.GetDuration<MetricTimeSpan>();

            Debug.Log($"Duration: {duration.TotalSeconds}");

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

            // -------------------------------------

            List<TickInfo> ticks = new();

            //Last set tick
            TickInfo lastTick = new TickInfo();
            //Scheduled next tick, used for tempo change
            TickInfo scheduledTick = new TickInfo();

            void AddTick(double time, double bpm)
            {
                var info = new TickInfo()
                {
                    Time = time,
                    BPM = bpm
                };

                lastTick = info;
                ticks.Add(info);
            }

            void AddTickAgain()
            {
                lastTick = scheduledTick;
                ticks.Add(scheduledTick);
            }

            void ScheduleTick(double t, double bpm)
            {
                scheduledTick.Time = t;
                scheduledTick.BPM = bpm;
            }
            void ScheduleTickAgain()
            {
                scheduledTick.Time = lastTick.Time + lastTick.BPM_Seconds;
                scheduledTick.BPM = lastTick.BPM;
            }

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

                
                // Initial beat
                if (isFirst)
                {
                    AddTick(0, current.BPM);
                    ScheduleTickAgain();
                }

                bool hasSegmentTicks = false;

                //Solve current segment and schedule a tempo change if needed
                while(true)
                {
                    bool scheduledTickIsInSegment = scheduledTick.Time <= next.Time;

                    if (scheduledTickIsInSegment) //no tempo change
                    {
                        AddTickAgain();
                        ScheduleTickAgain();

                        hasSegmentTicks = true;
                    }
                    else if (isLast) // last segment exit
                    {
                        break;
                    }
                    else // Compensating tempo change 
                    {
                        double diff = scheduledTick.Time - next.Time;
                        double tempoChange = diff * (next.BPM / lastTick.BPM);

                        ScheduleTick(next.Time + tempoChange, next.BPM);

                        break;
                    }

                    //Assert
                    if (lastTick.Time == scheduledTick.Time)
                        throw new Exception($"Loop prevented! Aborting metronome tick generation.");
                }
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