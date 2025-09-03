using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TankFinity
{
    public static class TerainLogic
    {
        public static int BYTES_PER_PIXEL = 4;

        public unsafe static void GenerateTerrain(TerrainData terrainData, byte[] m_foregroundColor, int terrainSection, int numSections)
        {
            float* terrainHeight = stackalloc float[terrainData.TextureWidth];

            terrainSection *= numSections;
            int section = terrainData.TextureWidth / numSections;
            float y1 = 0.0f;
            float y4 = 0.0f;
            for (int i = 0; i < numSections; i++)
            {
                int x1 = section * i;
                int x2 = section * (i + 1);
                y1 = (i == 2 || i == 7) ? y1 : terrainData.RandomTable[(terrainSection + i) % terrainData.RandomTable.Length];
                y4 = (i == 1 || i == 6) ? y1 : terrainData.RandomTable[(terrainSection + i + 1) % terrainData.RandomTable.Length];
                float y2 = y1;
                float y3 = y4;
                for (int x = x1; x < x2; x++)
                {
                    float t = (x - x1) / (float)(x2 - x1);
                    float y = (Mathf.Pow(1 - t, 3) * y1) + ((3 * Mathf.Pow(1 - t, 2) * t * y2)) + (3 * (1 - t) * Mathf.Pow(t, 2) * y3 + Mathf.Pow(t, 3) * y4);
                    terrainHeight[x] = y;
                }
            }

            // colors
            for (int i = 0; i < terrainData.Pixels.Length; i += BYTES_PER_PIXEL)
            {
                terrainData.Pixels[i + 3] = 0x0;
            }

            for (int i = 0; i < terrainData.TextureWidth; i++)
            {
                int indexI = (terrainData.TextureWidth - 1 - i) * terrainData.TextureWidth;
                int maxJ = (int)Mathf.Floor(terrainHeight[i] * terrainData.TextureWidth);

                for (int j = 0; j < maxJ / 4; j++)
                {
                    int index = indexI + j;

                    float ratio = (float)j / ((float)maxJ) + 0.75f;

                    terrainData.Pixels[index * BYTES_PER_PIXEL + 0] = (byte)(m_foregroundColor[0] * ratio);
                    terrainData.Pixels[index * BYTES_PER_PIXEL + 1] = (byte)(m_foregroundColor[1] * ratio);
                    terrainData.Pixels[index * BYTES_PER_PIXEL + 2] = (byte)(m_foregroundColor[2] * ratio);
                    terrainData.Pixels[index * BYTES_PER_PIXEL + 3] = 0xff;// = (byte)(foregroundColor[3] * ratio);
                }
                for (int j = maxJ / 4; j < maxJ; j++)
                {
                    int index = indexI + j;

                    terrainData.Pixels[index * BYTES_PER_PIXEL + 0] = m_foregroundColor[0];
                    terrainData.Pixels[index * BYTES_PER_PIXEL + 1] = m_foregroundColor[1];
                    terrainData.Pixels[index * BYTES_PER_PIXEL + 2] = m_foregroundColor[2];
                    terrainData.Pixels[index * BYTES_PER_PIXEL + 3] = 0xff;// = (byte)(foregroundColor[3] * ratio);
                }
            }
        }

        public static bool CheckCollision(TerrainData terrainData, float x, float y)
        {
            int tx = (int)((1.0f - x) * terrainData.TextureWidth);
            int ty = (int)(y * terrainData.TextureWidth);
            int index = (tx * terrainData.TextureWidth) + ty;
            int pIndex = index * BYTES_PER_PIXEL + 3;
            if (pIndex < 0 || pIndex >= terrainData.Pixels.Length)
                return false;
            return (terrainData.Pixels[pIndex] == 0xff);

        }

        public static void Explode(TerrainData terrainData, float x, float y, int radius)
        {
            int tx = (int)((1.0f - x) * terrainData.TextureWidth);
            int ty = (int)(y * terrainData.TextureWidth);
            int rSquared = radius * radius;
            radius = (int)(radius * 1.2f);
            int rSquared2 = radius * radius;

            for (int u = tx - radius; u < tx + radius + 1; u++)
                for (int v = ty - radius; v < ty + radius + 1; v++)
                    if ((u >= 0 && u < terrainData.TextureWidth) && (v >= 0 && v <= terrainData.TextureWidth))
                    {
                        int u2 = u * terrainData.TextureWidth;
                        int xDiff = (tx - u);
                        int yDiff = (ty - v);
                        int dSquared = xDiff * xDiff + yDiff * yDiff;
                        if (dSquared < rSquared)
                        {
                            int index = u2 + v;
                            terrainData.Pixels[index * BYTES_PER_PIXEL + 0] = 0xFF;
                            terrainData.Pixels[index * BYTES_PER_PIXEL + 1] = 0x0;
                            terrainData.Pixels[index * BYTES_PER_PIXEL + 2] = 0x0;
                            terrainData.Pixels[index * BYTES_PER_PIXEL + 3] = 0xFF;

                            terrainData.FireIndices[terrainData.FireCount] = index;
                            terrainData.FireTime[terrainData.FireCount] = 0.5f;
                            terrainData.FireCount++;
                        }
                        else if (dSquared < (rSquared2))
                        {
                            int index = u2 + v;
                            if (terrainData.Pixels[index * BYTES_PER_PIXEL + 3] == 0xFF)
                            {
                                float ratio = (float)(dSquared - rSquared) / (float)(rSquared2 - rSquared) * 0.5f + 0.5f;
                                terrainData.Pixels[index * BYTES_PER_PIXEL + 0] = (byte)(terrainData.Pixels[index * BYTES_PER_PIXEL + 0] * ratio);
                                terrainData.Pixels[index * BYTES_PER_PIXEL + 1] = (byte)(terrainData.Pixels[index * BYTES_PER_PIXEL + 1] * ratio);
                                terrainData.Pixels[index * BYTES_PER_PIXEL + 2] = (byte)(terrainData.Pixels[index * BYTES_PER_PIXEL + 2] * ratio);
                            }
                        }
                    }

        }

        public static bool DropSand(TerrainData terrainData)
        {
            bool dropped = false;
            for (int i = 0; i < 4; i++)
            {
                for (int tx = 0; tx < terrainData.TextureWidth; tx++)
                {
                    int tx2 = tx * terrainData.TextureWidth;
                    for (int ty = 0; ty < terrainData.TextureWidth - 1; ty++)
                    {
                        int index = tx2 + ty;
                        int index2 = tx2 + (ty + 1);
                        int pIndex = index * BYTES_PER_PIXEL + 3;
                        int pIndex2 = index2 * BYTES_PER_PIXEL + 3;
                        if (terrainData.Pixels[pIndex] == 0x0 && terrainData.Pixels[pIndex2] == 0xff)
                        {
                            terrainData.Pixels[index * BYTES_PER_PIXEL + 0] = terrainData.Pixels[index2 * BYTES_PER_PIXEL + 0];
                            terrainData.Pixels[index * BYTES_PER_PIXEL + 1] = terrainData.Pixels[index2 * BYTES_PER_PIXEL + 1];
                            terrainData.Pixels[index * BYTES_PER_PIXEL + 2] = terrainData.Pixels[index2 * BYTES_PER_PIXEL + 2];

                            terrainData.Pixels[pIndex] = 0xff;
                            terrainData.Pixels[pIndex2] = 0x0;
                            dropped = true;
                        }
                    }
                }
            }
            return dropped;
        }

        public static void Tick(TerrainData terrainData)
        {
            int count = 0;
            for (int i = 0; i < terrainData.FireCount; i++)
            {
                terrainData.FireTime[i] -= Time.deltaTime;
                int index = terrainData.FireIndices[i];
                if (terrainData.FireTime[i] > 0.0f)
                {
                    terrainData.Pixels[index * BYTES_PER_PIXEL + 0] = (terrainData.FireTime[i] >= 0.25f) ? (byte)0xFF : (byte)(terrainData.FireTime[i] * 4.0f * 255.0f);
                    terrainData.Pixels[index * BYTES_PER_PIXEL + 3] = (terrainData.FireTime[i] >= 0.125f) ? (byte)0xFF : (byte)(terrainData.FireTime[i] * 8.0f * 255.0f);

                    terrainData.FireTime[count] = terrainData.FireTime[i];
                    terrainData.FireIndices[count] = terrainData.FireIndices[i];
                    count++;
                }
                else
                {
                    terrainData.Pixels[index * BYTES_PER_PIXEL + 3] = 0x0;//foregroundColor;
                }
            }
            terrainData.FireCount = count;
        }

        public static float GetYPosForX(TerrainData TerrainData, float x, float localScaleY)
        {
            int tx = (int)((1.0f - x) * TerrainData.TextureWidth);
            for (int ty = TerrainData.TextureWidth - 1; ty >= 0; ty--)
            {
                int index = (tx * TerrainData.TextureWidth) + ty;
                int pIndex = index * BYTES_PER_PIXEL + 3;
                if (TerrainData.Pixels[pIndex] == 0xff)
                    return (localScaleY * (float)ty / (float)TerrainData.TextureWidth) - (localScaleY / 2.0f);
            }
            return -localScaleY / 2.0f;
        }
    }
}