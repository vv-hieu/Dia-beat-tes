using UnityEngine;

public static class Noise
{
    public static float WhiteNoise(int seed, float x, float y)
    {
        float hi = (seed >> 16) / 65536.0f;
        float lo = (seed & 0xffff) / 65536.0f;

        float val = Mathf.Sin((x + hi) * 12.9898f + (y + lo) * 78.233f);
        val *= 43758.5453f;
        val -= Mathf.Floor(val);

        return val;
    }

    public static float WhiteNoise(int seed, Vector2 pos)
    {
        return WhiteNoise(seed, pos.x, pos.y);
    }

    public static float[,] WhiteNoiseMap(Vector2Int mapSize, int seed)
    {
        float[,] res = new float[mapSize.x, mapSize.y];

        for (int x = 0; x < mapSize.x; ++x)
        {
            for (int y = 0; y < mapSize.y; ++y)
            {
                res[x, y] = WhiteNoise(seed, x / (mapSize.x - 1.0f), y / (mapSize.y - 1.0f));
            }
        }

        return res;
    }

    public static float[,] FractalNoiseMap(Vector2Int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity)
    {
        scale = Mathf.Max(0.0001f, scale);

        float hi = seed >> 16;
        float lo = seed & 0xffff;

        float[,] res = new float[mapSize.x, mapSize.y];

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int x = 0; x < mapSize.x; ++x)
        {
            for (int y = 0; y < mapSize.y; ++y)
            {
                res[x, y] = 0.0f;

                float amplitude = 1.0f;
                float frequency = 1.0f;

                float sampleX = x * scale;
                float sampleY = y * scale;

                for (int i = 0; i < octaves; ++i)
                {
                    res[x, y] += amplitude * (Mathf.PerlinNoise(sampleX * frequency + hi, sampleY * frequency + lo) * 2.0f - 1.0f);

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (res[x, y] < minValue)
                {
                    minValue = res[x, y];
                }
                else if (res[x, y] > maxValue)
                {
                    maxValue = res[x, y];
                }
            }
        }

        for (int x = 0; x < mapSize.x; ++x)
        {
            for (int y = 0; y < mapSize.y; ++y)
            {
                res[x, y] = Mathf.InverseLerp(minValue, maxValue, res[x, y]);
            }
        }

        return res;
    }

    public static float[,] RidgedNoiseMap(Vector2Int mapSize, int seed, float scale)
    {
        float[,] res = new float[mapSize.x, mapSize.y];

        float hi = seed >> 16;
        float lo = seed & 0xffff;

        for (int x = 0; x < mapSize.x; ++x)
        {
            for (int y = 0; y < mapSize.y; ++y)
            {
                float sampleX = x * scale + lo;
                float sampleY = y * scale + hi;

                res[x, y] = 1.0f - Mathf.Abs(Mathf.PerlinNoise(sampleX, sampleY) * 2.0f - 1.0f);
            }
        }

        return res;
    }

    // TODO: Ridged noise
}
