using MidiMetronome;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GeneralTests
{
    readonly string[] MidiTestFiles = new string[]
    {
        "MIDI_sample",
        "darude-sandstorm",
        "dr.dre-still",
        "Under-The-Sea-(From-'The-Little-Mermaid')"
    };

    [Test]
    public void Dev()
    {
        var assets = AssetDatabase.FindAssets(nameof(GeneralTests))
                                  .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                                  .Select(path => AssetDatabase.LoadAssetAtPath<MonoScript>(path))
                                  .Distinct();

        var script = assets.FirstOrDefault();

        if (script == null)
            throw new Exception($"Can't locate the script in the project!");

        var path = AssetDatabase.GetAssetPath(script);
        var root = Path.GetDirectoryName(path);
        var sampleDir = $@"{root}\Samples";

        var midiFiles = Directory.GetFiles(sampleDir, "*.mid")
                                 .Select(x => new { name = Path.GetFileName(x), rawMidi = File.ReadAllBytes(x) });

        foreach (var x in midiFiles)
            TestMidi(x.rawMidi, x.name);
    }
    void TestMidi(byte[] rawMidi, string name)
    {

        TickInfo[] ticks = null;

        try
        {
            ticks = MetronomeUtility.GenerateBeats(rawMidi);
        }
        catch (Exception e)
        {
            Debug.LogError(name);
            Debug.LogException(e);
        }

        if (ticks == null)
            return;

        Debug.Log($"{name}");

        foreach (var x in ticks)
        {
            Debug.Log($"[{x.BPM:F0}] {x.Time:0.###}s");
        }

        Debug.Log("======================");
    }
}
