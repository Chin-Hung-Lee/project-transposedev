﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapHandler : MonoBehaviour
{
    private readonly IReadOnlyCollection<string> maps;
    private readonly int numberOfRounds;

    private int currentRound;
    private List<string> remainingMaps;

    public MapHandler(MapSet mapSet, int numberOfRounds) {
        maps = mapSet.Maps;
        this.numberOfRounds = numberOfRounds;

        ResetMaps();
    }

    public bool IsComplete => currentRound == numberOfRounds;

    public string NextMap() {
        if (IsComplete) {
            return null;
        }

        if (remainingMaps.Count == 0) {
            ResetMaps();
        }

        string map = remainingMaps[UnityEngine.Random.Range(0, remainingMaps.Count)];

        remainingMaps.Remove(map);

        return map;
    }

    private void ResetMaps() => remainingMaps = maps.ToList();


}
