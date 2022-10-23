using MidiMetronome;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MidiMetronome.Tests
{
    public class GeneralTests
    {
        [Test]
        public void SampleData()
        {
            //Find script location
            var assets = AssetDatabase.FindAssets(nameof(GeneralTests))
                                      .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                                      .Select(path => AssetDatabase.LoadAssetAtPath<MonoScript>(path))
                                      .Distinct();

            var script = assets.FirstOrDefault();

            if (script == null)
                throw new Exception($"Can't locate the script in the project!");

            //Assamble samples folder path
            var path = AssetDatabase.GetAssetPath(script);
            var root = Path.GetDirectoryName(path);
            var sampleDir = $@"{root}\Samples";

            //Filter and load midis to memory
            var midiFiles = Directory.GetFiles(sampleDir, "*.mid")
                                     .Select(x => new { name = Path.GetFileName(x), rawMidi = File.ReadAllBytes(x) });

            //TestMidis for integrity
            foreach (var x in midiFiles)
                TestMidi(x.rawMidi, x.name);
        }
        void TestMidi(byte[] rawMidi, string name)
        {
            MetronomeInfo info = null;

            try
            {
                info = MetronomeUtility.GenerateBeats(rawMidi, BPMScaling.ScaleWithDenumerator);
            }
            catch (Exception e)
            {
                Debug.LogError(name);
                Debug.LogException(e);
            }

            if (info == null)
                throw new Exception("Utility have failed!");


            

            Debug.Log($"{name} - Beats:{info.Beats.Length}, Changes: {info.Changes.Length}, Measure beats: {info.Beats.Count(x => x.IsMeasureBeat)}");

            //Debug.Log($"{string.Join("\n", info.Changes.Select(x => $"[{x.BPM:F0}] - {x.TimeSignatureNumber}/{x.TimeSignatureDenumerator}"))}");

            Debug.Log("======================");
        }

        void TestTiming(TickInfo[] ticks)
        {
            for (int i = 0; i < Mathf.Clamp(ticks.Length - 1, 0, float.MaxValue); i++)
            {
                var current = ticks[i];
                var next = ticks[i + 1];

                Assert.IsTrue(Approximately(current.Time + current.BeatDuration, next.Time), $"Difference between {current.Time} and {next.Time} isn't {current.BeatDuration}");
            }
        }

        bool Approximately(double f1, double f2) => Mathf.Approximately((float)f1, (float)f2);
    }
}