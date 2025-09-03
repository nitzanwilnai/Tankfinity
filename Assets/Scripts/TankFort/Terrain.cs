using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace TankFinity
{
    public class TerrainData
    {
        public int TextureWidth;
        public byte[] Pixels;

        public int[] FireIndices;
        public float[] FireTime;
        public int FireCount = 0;

        public float[] RandomTable;
    }

    public class Terrain : MonoBehaviour
    {
        public TerrainData TerrainData = new TerrainData();

        int m_numSections = 8;
        public float LightFactor = 1.35f;

        byte[] m_backgroundColor = new byte[4];
        byte[] m_foregroundColor = new byte[4];

        Sprite m_terrainSprite = null;
        Texture2D m_terrainTexture = null;

        static int randomSeed;
        static float CustomRandFloat()
        {
            randomSeed = (214013 * randomSeed + 2531011);
            return (float)((randomSeed >> 16) & 0x7FFF) / (32768.0f);
        }

        private void Awake()
        {
            TerrainData.TextureWidth = BoardVisual.textureWidth;

            //terrainTexture = new Texture2D(textureWidth, textureWidth, TextureFormat.RGBA32, false);
            m_terrainTexture = new Texture2D(TerrainData.TextureWidth, TerrainData.TextureWidth);
            m_terrainSprite = Sprite.Create(m_terrainTexture, new Rect(0.0f, 0.0f, m_terrainTexture.width, m_terrainTexture.height), new Vector2(0.5f, 0.5f), 1024);
            GetComponent<SpriteRenderer>().sprite = m_terrainSprite;

            //Texture2D currentTexture = GetComponent<SpriteRenderer>().sprite.texture;
            //currentTexture.LoadRawTextureData(terrainTexture.GetRawTextureData());

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            TerrainData.Pixels = m_terrainTexture.GetRawTextureData();
            spriteRenderer.material.mainTexture = m_terrainTexture;

            randomSeed = 0;
            TerrainData.RandomTable = new float[16384];
            for (int i = 0; i < TerrainData.RandomTable.Length; i++)
                TerrainData.RandomTable[i] = CustomRandFloat() * 0.6f + 0.1f;

            TerrainData.FireIndices = new int[TerrainData.TextureWidth * TerrainData.TextureWidth];
            TerrainData.FireTime = new float[TerrainData.TextureWidth * TerrainData.TextureWidth];

        }

        // Use this for initialization
        void Start()
        {

        }

        public void GenerateTerrainSprite(int terrainSection, Color terrainColor)
        {
            m_backgroundColor[0] = (byte)(terrainColor.r * 255.0f * LightFactor);
            m_backgroundColor[1] = (byte)(terrainColor.g * 255.0f * LightFactor);
            m_backgroundColor[2] = (byte)(terrainColor.b * 255.0f * LightFactor);
            m_backgroundColor[3] = 0x0;
            m_foregroundColor[0] = (byte)(terrainColor.r * 255.0f);
            m_foregroundColor[1] = (byte)(terrainColor.g * 255.0f);
            m_foregroundColor[2] = (byte)(terrainColor.b * 255.0f);
            m_foregroundColor[3] = 0xff;

            float timerStart = Time.realtimeSinceStartup;
            float timerDelta = Time.realtimeSinceStartup;

            TerainLogic.GenerateTerrain(TerrainData, m_foregroundColor, terrainSection, m_numSections);
            //Debug.LogFormat("GenerateTerrain done {0}", Time.realtimeSinceStartup - timerDelta);

            timerDelta = Time.realtimeSinceStartup;
            m_terrainTexture.LoadRawTextureData(TerrainData.Pixels);
            //Debug.LogFormat("terrainTexture.LoadRawTextureData {0}", Time.realtimeSinceStartup - timerDelta);

            timerDelta = Time.realtimeSinceStartup;
            m_terrainTexture.Apply();
            //Debug.LogFormat("terrainTexture.Apply {0}", Time.realtimeSinceStartup - timerDelta);

            timerDelta = Time.realtimeSinceStartup;
            GetComponent<SpriteRenderer>().material.mainTexture = m_terrainTexture;
            //Debug.LogFormat("assign texture to material {0}", Time.realtimeSinceStartup - timerDelta);
            //Debug.LogFormat("GenerateTerrainSprite() TOTAL TIME {0}", Time.realtimeSinceStartup - timerStart);
        }

        public float GetYPosForX(float x)
        {
            return TerainLogic.GetYPosForX(TerrainData, x, transform.localScale.y);
        }

        private void convertLocalToTerrain(ref float x, ref float y)
        {
            x += transform.localScale.x / 2.0f;
            y += transform.localScale.y / 2.0f;
            x /= transform.localScale.x;
            y /= transform.localScale.y;
        }

        public bool CheckCollision(float x, float y)
        {
            convertLocalToTerrain(ref x, ref y);

            return TerainLogic.CheckCollision(TerrainData, x, y);
        }

        public void Explode(float x, float y, int radius)
        {
            convertLocalToTerrain(ref x, ref y);

            TerainLogic.Explode(TerrainData, x, y, radius);

            m_terrainTexture.LoadRawTextureData(TerrainData.Pixels);
            m_terrainTexture.Apply();
        }

        private void Update()
        {
            TerainLogic.Tick(TerrainData);

            m_terrainTexture.LoadRawTextureData(TerrainData.Pixels);
            m_terrainTexture.Apply();
        }
    }
}