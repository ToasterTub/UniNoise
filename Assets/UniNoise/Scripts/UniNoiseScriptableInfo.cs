using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniNoiseScriptableInfo : ScriptableObject
{
    public string saveLocation;

    public List<saveablePreset> presets = new List<saveablePreset>();
    public static string defaultSaveLocation()
    {
        return "Assets/UniNoise/Scripts/info.asset";
    }

    [System.Serializable]
    public class saveablePreset
    {
        public string name = "";
        public string genOrEditTypeName;
        public saveableGradient gradient;
        public List<presetVariable> variables = new List<presetVariable>();
    }

    [System.Serializable]
    public class saveableGradient
    {
        public saveableGradientAlphaKey[] alphaKeys;
        public saveableGradientColorKey[] colorKeys;
        public bool isBlend;
        public saveableGradient(Gradient G)
        {
            isBlend = G.mode == GradientMode.Blend;
            List<saveableGradientAlphaKey> alphas = new List<saveableGradientAlphaKey>();
            List<saveableGradientColorKey> colors = new List<saveableGradientColorKey>();

            foreach(GradientAlphaKey I in G.alphaKeys)
            {
                alphas.Add(new saveableGradientAlphaKey(I));
            }
            foreach(GradientColorKey I in G.colorKeys)
            {
                colors.Add(new saveableGradientColorKey(I));
            }

            alphaKeys = alphas.ToArray();
            colorKeys = colors.ToArray();
        }
        public Gradient getGradient()
        {
            List<GradientAlphaKey> alphas = new List<GradientAlphaKey>();
            List<GradientColorKey> colors = new List<GradientColorKey>();
            foreach (saveableGradientAlphaKey I in alphaKeys)
            {
                alphas.Add(I.getKey());
            }
            foreach (saveableGradientColorKey I in colorKeys)
            {
                colors.Add(I.getKey());
            }

            Gradient G = new Gradient();
            G.mode = isBlend ? GradientMode.Blend : GradientMode.Fixed;
            G.alphaKeys = alphas.ToArray();
            G.colorKeys = colors.ToArray();
            return G;
        }
    }

    [System.Serializable]
    public class saveableGradientAlphaKey
    {
        public float time;
        public float alpha;
        public saveableGradientAlphaKey(GradientAlphaKey key)
        {
            time = key.time;
            alpha = key.alpha;
        }
        public GradientAlphaKey getKey()
        {
            return new GradientAlphaKey(alpha, time);
        }
    }

    [System.Serializable]
    public class saveableGradientColorKey
    {
        public string color;
        public float time;
        public saveableGradientColorKey(GradientColorKey key)
        {
            time = key.time;
            color = ColorUtility.ToHtmlStringRGB(key.color);
        }
        public GradientColorKey getKey()
        {
            GradientColorKey K = new GradientColorKey();
            K.time = time;
            ColorUtility.TryParseHtmlString("#"+color, out Color C);
            K.color = C;
            return K;
        }
    }

    [System.Serializable]
    public class presetVariable{
        
        public string variableName;
        public string variableType;
        public string variableValue;

        public presetVariable(string name, string value, string type)
        {
            variableType = type;
            variableName = name;
            variableValue = value;
        }
    }
}
