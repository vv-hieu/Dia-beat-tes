using UnityEngine;

public static class Falloff
{
    public static float[,] FalloffMap(Vector2Int mapSize)
    {
        float[,] res = new float[mapSize.x, mapSize.y];

        for (int x = 0; x < mapSize.x; ++x)
        {
            for (int y = 0; y < mapSize.y; ++y)
            {
                float xValue = (float)x / (mapSize.x - 1) * 2.0f - 1.0f;
                float yValue = (float)y / (mapSize.y - 1) * 2.0f - 1.0f;

                res[x, y] = p_Evaluate(Mathf.Max(Mathf.Abs(xValue), Mathf.Abs(yValue)));
            }
        }

        return res;
    }

    private static float p_Evaluate(float value)
    {
        float a = 3.0f;
        float b = 2.2f;

        float xa   = Mathf.Pow(value, a);
        float bbxa = Mathf.Pow(b - b * value, a);

        return xa / (xa + bbxa);
    }
}
