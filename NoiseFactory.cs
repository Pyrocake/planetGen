using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseFactory{

    public static INoiseFilter CreateNoiseFilter(LegacyNoiseSettings settings)
    {
        switch (settings.filterType)
        {
            case LegacyNoiseSettings.FilterType.Legacy:
                return new LegacyNoiseFilter(settings.legacySimpleNoiseSettings);
            case LegacyNoiseSettings.FilterType.Rigid:
                return new RigidNoiseFilter(settings.rigidNoiseSettings);
        }
        return null;
    }

    internal static object CreateNoiseFilter(object noiseSettings) {
        throw new NotImplementedException();
    }
}
