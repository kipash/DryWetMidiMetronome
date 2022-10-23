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
    public enum BPMScaling
    {
        DoNotScaleWithDenumerator,
        ScaleWithDenumerator
    }

    public static class MetronomeUtility
    {
        public static MetronomeInfo GenerateBeats(string path, BPMScaling scaling) => GenerateBeats(MidiFile.Read(path), scaling);
        public static MetronomeInfo GenerateBeats(byte[] raw, BPMScaling scaling)
        {
            using (MemoryStream ms = new MemoryStream(raw))
                return GenerateBeats(MidiFile.Read(ms), scaling);
        }
        public static MetronomeInfo GenerateBeats(MidiFile midi, BPMScaling scaling)
        {
            var info = new MetronomeInfo();
            var tempoMap = midi.GetTempoMap();

            //Total duration of the midi file
            TimeSpan duration = midi.GetDuration<MetricTimeSpan>();

            //Buffer to contain all 
            TickInfo[] changes = null;

            var rawTempoChanges = tempoMap.GetTempoChanges();
            var rawTimeSignatureChanges = tempoMap.GetTimeSignatureChanges();

            changes = rawTempoChanges.Select(x => TickInfo.Create(x, tempoMap, scaling))
                                     .ToArray();

            if (changes == null)
            {
                var initialTempo = tempoMap.GetTempoAtTime(new MetricTimeSpan());

                changes = new TickInfo[] {
                    TickInfo.Create(0, initialTempo.BeatsPerMinute, tempoMap, scaling)
                };
            }

            // -------------------------------------

            List<TickInfo> ticks = new();

            //Last set tick
            TickInfo lastTick = new();
            //Scheduled next tick, used for tempo change
            TickInfo scheduledTick = new();

            void AddTick(double time, double bpm)
            {
                var info = TickInfo.Create(time, bpm, tempoMap, scaling);

                lastTick = info;
                ticks.Add(info);
            }

            void AddScheduledTick() => AddTick(scheduledTick.Time, scheduledTick.BPM);

            void ScheduleTick(double t, double bpm)
            {
                scheduledTick = TickInfo.Create(t, bpm, tempoMap, scaling);
            }
            void ScheduleTickBasedOnLast()
            {
                scheduledTick = TickInfo.Create(lastTick.Time + lastTick.BeatDuration, lastTick.BPM, tempoMap, scaling);
            }

            for (int i = 0; i < changes.Length; i++)
            {
                bool isLast = (i + 1) >= changes.Length;
                bool isFirst = i == 0;

                TickInfo current = changes[i];
                TickInfo next = new TickInfo(); //default state

                if (isLast)
                    next = TickInfo.Create(duration.TotalSeconds, current.BPM, tempoMap, scaling);
                else
                    next = changes[i + 1];

                //Debug.Log($"CHANGE: {current.Time} - {current.ScaledBPM:F0} - {current.TimeSignatureNumber}/{current.TimeSignatureDenumerator}");

                // Initial beat
                if (isFirst)
                {
                    AddTick(0, current.BPM);
                    ScheduleTickBasedOnLast();
                }

                //Solve current segment and schedule a tempo change if needed
                while (true)
                {
                    bool scheduledTickIsInSegment = scheduledTick.Time <= next.Time;

                    if (scheduledTickIsInSegment) //no tempo change
                    {
                        //Debug.Log($"ADD {scheduledTick.Time}s = bpm:{scheduledTick.BPM:F0} | Takt:{scheduledTick.TimeSignatureDenumerator} | Beat_Dur: {scheduledTick.BeatDuration}");
                        AddScheduledTick();
                        ScheduleTickBasedOnLast();
                    }
                    else if (isLast) // last segment exit
                    {
                        break;
                    }
                    else // Compensating tempo change 
                    {
                        double diff = scheduledTick.Time - next.Time;
                        double tempoChange = diff * (next.ScaledBPM / lastTick.ScaledBPM);

                        ScheduleTick(next.Time + tempoChange, next.BPM);

                        break;
                    }

                    //Assert
                    if (lastTick.Time == scheduledTick.Time || lastTick.BeatDuration == 0)
                        throw new Exception($"Loop prevented! Aborting metronome tick generation.");
                }
            }

            //Mark Measure Beats
            int beatCount = 0;
            for (int i = 0; i < ticks.Count; i++)
            {
                if (ticks[i].TimeSignatureNumber <= beatCount || i == 0)
                {
                    var tick = ticks[i];
                    tick.IsMeasureBeat = true;
                    ticks[i] = tick;

                    beatCount = 0;
                }
                beatCount++;
            }

            info.Beats = ticks.ToArray();
            info.Changes = changes;

            return info;
        }
        public static SortedList<float, TickInfo> ComposeSortedList(MetronomeInfo info)
        {
            var list = new SortedList<float, TickInfo>();

            foreach(var x in info.Beats)
            {
                list.Add((float)x.Time, x);
            }

            return list;
        }
    }
}