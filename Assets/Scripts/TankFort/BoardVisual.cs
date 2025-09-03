using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace TankFinity
{
    public class BoardVisual : MonoBehaviour
    {
        public static int textureWidth = 1024;

        public enum GAME_STATE { PRIVACY_POLICY, READY_FOR_INPUT, FIRING, EXPLOSION, DROP_SAND, DROP_CASTLE, CASTLE_HIT, SCROLL };
        public GAME_STATE GameState = GAME_STATE.PRIVACY_POLICY;

        public Camera Camera;
        public Terrain[] Terrain;
        public GameObject Ground;
        public Color[] TerrainColor;
        float m_terrainWidth;

        public Text shotsText;
        public Text avgText;

        public float scrollSpeed = 0.0f;

        public Tank[] tank;
        public Vector3[] tankTargetPos;
        public Quaternion[] tankTargetRot;

        public Sprite[] castleSprites;
        public Sprite castleDestroyed;
        public GameObject[] castle;
        public GameObject[] debris;

        public LineRenderer aimLine;
        public GameObject aimStart;
        public GameObject aimEnd;

        public AudioSource audioSource;
        public AudioClip sfxFire;
        public AudioClip sfxExplosion;
        public AudioClip sfxCastle;

        float terrainHalfSize = 0;

        int terrainIdx = 0;

        private bool mouseDown;
        private bool mouseUp;
        private bool mouseMove;
        private Vector3 mousePosition;
        private Vector3 mouseDownPosition;

        public Camera mainCamera;

        public GameObject bulletPrefab;
        public Transform poolParent;
        public Transform pathParent;

        GameObject[] bulletPool = new GameObject[16];
        int bulletCount = 0;
        Vector2[] bulletDir = new Vector2[16];
        Vector2[] bulletPos = new Vector2[16];
        int[] bulletIdx = new int[16];

        GameObject[] bulletPath = new GameObject[8];

        int bulletRadius = 64;

        // debris
        Vector2[] debrisDir;
        Vector2[] debrisPos;
        float[] debrisRot;
        bool[] debrisCheckC;
        float[] debrisTimer;
        int debrisCount = 0;

        float tankOffset;
        float castleOffset;
        bool castleHit;

        int screenshotCounter = 0;

        public void SetGameState(GAME_STATE newGameState)
        {
            GameState = newGameState;
        }

        // Use this for initialization
        public void Init(GameData gameData)
        {
            m_terrainWidth = Camera.ViewportToWorldPoint(new Vector2(1.0f, 1.0f)).x * 2.0f;

            for (int i = 0; i < Terrain.Length; i++)
            {
                Terrain[i].transform.localScale = new Vector3(m_terrainWidth, m_terrainWidth, 1.0f);
                Terrain[i].GenerateTerrainSprite(gameData.CurrentLevel + i, ColorNextLevel(gameData.CurrentLevel));

                tank[i].transform.localScale = new Vector3(m_terrainWidth / 75.0f, m_terrainWidth / 75.0f, 1.0f);
                castle[i].transform.localScale = new Vector3(m_terrainWidth / 20.0f, m_terrainWidth / 20.0f, 1.0f);

                PlaceObjectOnTerrain(i, tank[i].gameObject, 0.1875f);
                PlaceObjectOnTerrain(i, castle[i], 0.8125f);

                ShowCastle(i, gameData.CurrentLevel + i);
            }

            Terrain[1].transform.localPosition = new Vector3(m_terrainWidth, 0.0f, 0.0f);
            terrainHalfSize = m_terrainWidth / 2.0f;

            int numDebris = debris.Length;
            debrisDir = new Vector2[numDebris];
            debrisPos = new Vector2[numDebris];
            debrisRot = new float[numDebris];
            debrisCheckC = new bool[numDebris];
            debrisTimer = new float[numDebris];
            for (int i = 0; i < debris.Length; i++)
            {
                debris[i].transform.localScale = new Vector3(m_terrainWidth / 75.0f, m_terrainWidth / 75.0f, 1.0f);
                debris[i].SetActive(false);
            }


            tankOffset = Terrain[0].transform.position.x - tank[0].transform.position.x;
            castleOffset = Terrain[0].transform.position.x - castle[0].transform.position.x;
            tank[1].transform.position += new Vector3(m_terrainWidth, 0.0f, 0.0f);
            castle[1].transform.position += new Vector3(m_terrainWidth, 0.0f, 0.0f);

            Ground.transform.localScale = new Vector3(m_terrainWidth, m_terrainWidth, 1.0f);
            Ground.transform.localPosition = new Vector3(0.0f, -m_terrainWidth, 1.0f);

            int poolLength = bulletPool.Length;
            for (int i = 0; i < poolLength; i++)
            {
                GameObject tempBullet = Instantiate(bulletPrefab);
                tempBullet.SetActive(false);
                tempBullet.transform.SetParent(poolParent);
                tempBullet.transform.localPosition = Vector3.zero;
                tempBullet.transform.localScale = Vector3.one * (m_terrainWidth / 50.0f);
                bulletPool[i] = tempBullet;
            }

            int pathLength = bulletPath.Length;
            for (int i = 0; i < pathLength; i++)
            {
                GameObject tempBullet = Instantiate(bulletPrefab);
                tempBullet.SetActive(false);
                tempBullet.transform.SetParent(pathParent);
                tempBullet.transform.localPosition = Vector3.zero;
                tempBullet.transform.localScale = Vector3.one * (m_terrainWidth / 50.0f);
                bulletPath[i] = tempBullet;
            }

            castleHit = false;

            UpdateUI(gameData);
        }

        public void StartGame()
        {
            SetGameState(GAME_STATE.READY_FOR_INPUT);
        }

        Color ColorNextLevel(int currentLevel)
        {
            Color groundColor = GetColorForLevel(currentLevel - 1) * 0.75f;
            groundColor.a = 1.0f;
            Ground.GetComponent<SpriteRenderer>().color = groundColor;

            Color color = GetColorForLevel(currentLevel);
            Camera.backgroundColor = color * 1.3f;
            return color;
        }

        int colorMultiplier = 200;
        Color GetColorForLevel(int level)
        {
            int index = (int)(level / colorMultiplier);
            Color color1 = TerrainColor[index % TerrainColor.Length];
            Color color2 = TerrainColor[(index + 1) % TerrainColor.Length];
            float pct = ((float)(level % colorMultiplier) / (float)colorMultiplier);
            return (1.0f - pct) * color1 + pct * color2;
        }

        void UpdateUI(GameData gameData)
        {
            shotsText.text = "Shots:\n" + gameData.CurrentShots.ToString("N0") + ((gameData.ShotsThisLevel > 0) ? (" +" + gameData.ShotsThisLevel) : "");
            avgText.text = "Avg:\n" + ((gameData.CurrentShots > 0) ? ((float)gameData.CurrentShots / (float)(gameData.CurrentLevel)).ToString("N2") : "");

            PlayerPrefs.SetInt("CurrentLevel", gameData.CurrentLevel);
            PlayerPrefs.SetInt("CurrentShots", gameData.CurrentShots);

            aimLine.gameObject.SetActive(false);
            aimStart.SetActive(false);
            aimEnd.SetActive(false);
        }

        void PlaceObjectOnTerrain(int tIndex, GameObject gObject, float pos)
        {
            float posX = m_terrainWidth * (pos - 0.5f);
            float posY = Terrain[tIndex].GetYPosForX(pos);
            gObject.transform.localPosition = new Vector3(posX, posY, -1.0f);
        }

        // Update is called once per frame
        public void Tick(GameData gameData, float dt)
        {
            if (GameState == GAME_STATE.SCROLL)
            {
                for (int tIndex = 0; tIndex < Terrain.Length; tIndex++)
                {
                    Vector3 currentPosition = Terrain[tIndex].transform.localPosition;
                    currentPosition.x += scrollSpeed;
                    Terrain[tIndex].transform.localPosition = currentPosition;

                    if (Terrain[tIndex].transform.localPosition.x < -m_terrainWidth)
                    {
                        Terrain[tIndex].GenerateTerrainSprite(gameData.CurrentLevel + 1, ColorNextLevel(gameData.CurrentLevel));
                        SnapTerrain(tIndex, gameData.CurrentLevel);

                        terrainIdx = (tIndex + 1) % 2;
                        Terrain[terrainIdx].transform.localPosition = Vector3.zero;


                        SetGameState(GAME_STATE.READY_FOR_INPUT);
                        break;
                    }
                    else
                    {
                        Vector3 tankPos = tank[tIndex].transform.position;
                        tankPos.x = Terrain[tIndex].transform.position.x - tankOffset;
                        tank[tIndex].transform.position = tankPos;

                        Vector3 castlePos = castle[tIndex].transform.position;
                        castlePos.x = Terrain[tIndex].transform.position.x - castleOffset;
                        castle[tIndex].transform.position = castlePos;
                    }
                }
            }


            if (GameState == GAME_STATE.READY_FOR_INPUT)
                HandleInput(gameData);


            // move bullets
            if (GameState == GAME_STATE.FIRING)
            {
                MoveBullets(gameData, dt);
            }

            if (GameState == GAME_STATE.EXPLOSION)
            {
                if (Terrain[terrainIdx].TerrainData.FireCount == 0)
                    SetGameState(GAME_STATE.DROP_SAND);
            }

            if (GameState == GAME_STATE.DROP_SAND)
            {
                bool objectDropped = false;

                float tankY = Terrain[terrainIdx].GetYPosForX(0.1875f);
                float castleY = Terrain[terrainIdx].GetYPosForX(0.8125f);

                Vector3 tankV = tank[terrainIdx].transform.position;
                tankV.y = tankY;
                Vector3 castleV = castle[terrainIdx].transform.position;
                castleV.y = castleY;
                if (Mathf.Abs(tank[terrainIdx].transform.position.y - tankY) > 0.01f)
                {
                    objectDropped = true;
                    tank[terrainIdx].transform.position = Vector3.Lerp(tank[terrainIdx].transform.position, tankV, Time.deltaTime * 2.0f);
                }
                if (!castleHit && Mathf.Abs(castle[terrainIdx].transform.position.y - castleY) > 0.01f)
                {
                    objectDropped = true;
                    castle[terrainIdx].transform.position = Vector3.Lerp(castle[terrainIdx].transform.position, castleV, Time.deltaTime * 2.0f);
                }

                if (TerainLogic.DropSand(Terrain[terrainIdx].TerrainData))
                    objectDropped = true;

                if (!objectDropped && debrisCount == 0)
                {
                    PlaceObjectOnTerrain(terrainIdx, tank[terrainIdx].gameObject, 0.1875f);
                    PlaceObjectOnTerrain(terrainIdx, castle[terrainIdx], 0.8125f);
                    SetGameState(castleHit ? GAME_STATE.SCROLL : GAME_STATE.READY_FOR_INPUT);
                }
            }

            MoveDebris();
        }

        void MoveDebris()
        {
            debrisCount = 0;
            for (int i = 0; i < debris.Length; i++)
            {
                if (debris[i].activeSelf)
                {
                    if (debrisTimer[i] > 0.0f)
                    {
                        debrisTimer[i] -= Time.deltaTime;
                        if (debrisTimer[i] <= 0.0f)
                            debris[i].SetActive(false);
                    }
                    else if (Terrain[terrainIdx].TerrainData.FireCount == 0)
                        debrisCheckC[i] = true;

                    debrisPos[i] += debrisDir[i] * Time.deltaTime;
                    debrisDir[i].y -= (debrisCheckC[i] ? 2.5f : 1.0f) * Time.deltaTime;
                    debrisDir[i].y = Mathf.Max(debrisDir[i].y, -2.5f);
                    if (debrisCheckC[i] && debrisPos[i].y < terrainHalfSize && Terrain[terrainIdx].CheckCollision(debrisPos[i].x, debrisPos[i].y))
                    {
                        debrisDir[i] = Vector2.zero;
                        debrisCheckC[i] = false;
                        debrisTimer[i] = 1.0f;
                        debrisRot[i] = 0.0f;
                    }
                    else if (debrisPos[i].x > -m_terrainWidth && debrisPos[i].x < m_terrainWidth && debrisPos[i].y > -terrainHalfSize)
                    {
                        debrisCount++;
                        debris[i].transform.position = new Vector3(debrisPos[i].x, debrisPos[i].y, 1.0f);
                        Quaternion rotation = Quaternion.Euler(0.0f, 0.0f, debris[i].transform.localRotation.eulerAngles.z + debrisRot[i]);
                        debris[i].transform.localRotation = rotation;
                    }
                    else
                        debris[i].SetActive(false);
                }
            }
        }

        void SnapTerrain(int tIndex, int currentLevel)
        {
            for (int i = 0; i < 2; i++)
            {
                PlaceObjectOnTerrain(i, tank[i].gameObject, 0.1875f);
                PlaceObjectOnTerrain(i, castle[i], 0.8125f);
            }

            Vector3 currentPosition = Terrain[tIndex].transform.localPosition;
            currentPosition.x = m_terrainWidth;
            Terrain[tIndex].transform.localPosition = currentPosition;

            tank[tIndex].transform.position += new Vector3(m_terrainWidth, 0.0f, 0.0f);
            castle[tIndex].transform.position += new Vector3(m_terrainWidth, 0.0f, 0.0f);

            ShowCastle(tIndex, currentLevel + 1);
        }

        private void HandleInput(GameData gameData)
        {
            mouseDown = Input.GetMouseButtonDown(0);
            mouseUp = Input.GetMouseButtonUp(0);
            mouseMove = Input.GetMouseButton(0);
            mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (mouseDown)
            {
                mouseDownPosition = mousePosition;

                Bounds bounds = Terrain[terrainIdx].GetComponent<SpriteRenderer>().bounds;

                float x = ((mouseDownPosition.x + bounds.extents.x) - bounds.center.x) / (bounds.extents.x * 2);

                //Debug.LogFormat("mouseDownPosition {0} bounds {1} (x,y) {2},{3}", mouseDownPosition, bounds, x, y);
                if (x > 0.0f && x < 1.0f)
                {
                    float yPos = Terrain[terrainIdx].GetYPosForX(x);
                    bulletPrefab.transform.position = new Vector3(mousePosition.x, yPos, 0.0f);
                    bulletPrefab.transform.localPosition = new Vector3(bulletPrefab.transform.localPosition.x, bulletPrefab.transform.localPosition.y, -1.0f);
                }

                aimStart.SetActive(true);
                Vector3 aimPos = mouseDownPosition;
                aimPos.z = 0.0f;
                aimStart.transform.position = aimPos;
            }
            else if (mouseUp && Mathf.Approximately(mousePosition.z, mouseDownPosition.z))
            {
                Vector3 mouseDiff = mouseDownPosition - mousePosition;
                if (mouseDiff.y > 0.0f)
                    mouseDiff.y = 0.0f;

                Vector3 turretDir = (-tank[terrainIdx].TurretRotate.transform.right * mouseDiff.magnitude);
                FireBullet(gameData, tank[terrainIdx].TurretRotate.transform.position, turretDir);

                HidePreviewShot();
                aimStart.SetActive(false);
                aimLine.gameObject.SetActive(false);
            }

            if (mouseMove)
            {
                Vector3 mouseDiff = mouseDownPosition - mousePosition;

                Vector3 horizontal = tank[terrainIdx].transform.right;

                float turretAngle = -Mathf.Acos(Vector3.Dot(horizontal, mouseDiff.normalized)) * Mathf.Rad2Deg;
                Vector3 tangent = Vector3.Cross(horizontal, mouseDiff.normalized);

                turretAngle += 180.0f;
                if (tangent.z > 0.0f)
                    turretAngle = (turretAngle < 90.0f) ? 0.0f : 180.0f;

                Quaternion target = Quaternion.Euler(0, 0, turretAngle);
                tank[terrainIdx].TurretRotate.transform.localRotation = target;

                Vector3 turretDir = (-tank[terrainIdx].TurretRotate.transform.right * mouseDiff.magnitude);
                PreviewShot(tank[terrainIdx].FirePoint.transform.position, turretDir);

                Vector3 aimPos = mousePosition;
                aimPos.z = 0.0f;
                if (!aimLine.gameObject.activeSelf)
                {
                    aimLine.gameObject.SetActive(true);
                    aimEnd.gameObject.SetActive(true);
                }
                Vector3 aimLine0 = mouseDownPosition - mouseDiff.normalized * aimStart.transform.localScale.x;
                aimLine0.z = 0.0f;
                aimLine.SetPosition(0, aimLine0);
                aimLine.SetPosition(1, aimPos);
                aimEnd.transform.position = aimPos;
                float aimEndAngle = mouseDiff.normalized.y < -0.99f ? 0.0f : Mathf.Acos(Vector3.Dot(Vector3.down, mouseDiff.normalized)) * Mathf.Rad2Deg;
                Vector3 aimTanget = Vector3.Cross(new Vector3(0.0f, 1.0f, 0.0f), mouseDiff);
                if (aimTanget.z > 0.0f)
                    aimEndAngle = -aimEndAngle;
                aimEnd.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, aimEndAngle);

            }

#if UNITY_EDITOR
            if (Input.GetKeyDown("space") && GameState == GAME_STATE.READY_FOR_INPUT)
            {
                gameData.CurrentLevel++;
                UpdateUI(gameData);
                SetGameState(GAME_STATE.SCROLL);
            }
            if (Input.GetKeyDown("s"))
            {
                string screenshotString = Screen.width + "x" + Screen.height + "_" + screenshotCounter + ".png";
                screenshotCounter++;
                ScreenCapture.CaptureScreenshot(screenshotString, 1);
            }
#endif
        }

        void PreviewShot(Vector2 pos, Vector2 dir)
        {
            int numBullets = bulletPath.Length;
            float g = 0.0f;
            Vector2 p = pos;
            float dt = 1.0f / 60.0f;
            int multi = 10;
            for (int i = 0; i < numBullets * multi; i++)
            {
                p -= (dir * dt);
                dir.y += 1.0f * dt;

                if (i % multi == 0)
                {
                    int index = i / multi;
                    bulletPath[index].SetActive(true);
                    bulletPath[index].transform.position = p;
                }
            }
        }

        void HidePreviewShot()
        {
            int numBullets = bulletPath.Length;
            for (int i = 0; i < numBullets; i++)
            {
                bulletPath[i].SetActive(false);
            }
        }

        void FireBullet(GameData gameData, Vector2 pos, Vector2 dir)
        {
            int numBullets = bulletPool.Length;
            for (int i = 0; i < numBullets; i++)
            {
                if (!bulletPool[i].activeSelf)
                {
                    bulletPos[bulletCount] = pos;
                    bulletDir[bulletCount] = dir;
                    bulletIdx[bulletCount] = i;
                    bulletCount++;
                    bulletPool[i].transform.position = pos;
                    bulletPool[i].SetActive(true);
                    SetGameState(GAME_STATE.FIRING);

                    gameData.ShotsThisLevel++;
                    UpdateUI(gameData);

                    audioSource.PlayOneShot(sfxFire);

                    return;
                }
            }
        }

        void MoveBullets(GameData gameData, float dt)
        {
            castleHit = false;

            float halfTerrainWidth = m_terrainWidth / 2.0f;

            for (int k = 0; k < 2; k++)
            {
                for (int i = 0; i < bulletCount; i++)
                {
                    bulletPos[i] -= bulletDir[i] * dt;
                    bulletDir[i].y += 1.0f * dt;
                }

                // check bullet collision
                int count = 0;
                for (int i = 0; i < bulletCount; i++)
                {
                    float x = bulletPos[i].x;
                    float y = bulletPos[i].y;

                    bool explode = false;
                    Vector2 bPos = new Vector2(bulletPos[i].x, bulletPos[i].y);
                    Vector2 cPos = new Vector2(castle[terrainIdx].transform.position.x, castle[terrainIdx].transform.position.y);
                    float magnitude = (bPos - cPos).magnitude;
                    float radius = (float)64 / (float)textureWidth * m_terrainWidth;

                    if (magnitude <= radius)
                    {
                        castleHit = true;
                        DestroyCastle(gameData);
                        explode = true;
                    }
                    if (bulletPos[i].y < -halfTerrainWidth || bulletPos[i].y < halfTerrainWidth && Terrain[terrainIdx].CheckCollision(x, y))
                    {
                        radius = (float)bulletRadius / (float)textureWidth * m_terrainWidth;
                        if (magnitude <= radius)
                        {
                            castleHit = true;
                            DestroyCastle(gameData);
                        }
                        explode = true;
                    }
                    if (bulletPos[i].x < -halfTerrainWidth || bulletPos[i].x > halfTerrainWidth)
                        bulletPool[bulletIdx[i]].SetActive(false);
                    else if (!explode)
                    {
                        bulletPos[count] = bulletPos[i];
                        bulletDir[count] = bulletDir[i];
                        bulletIdx[count] = bulletIdx[i];
                        count++;
                    }

                    if (explode)
                    {
                        bulletPool[bulletIdx[i]].SetActive(false);
                        Terrain[terrainIdx].Explode(x, y, bulletRadius);
                        int otherTerrinIdx = (terrainIdx + 1) % Terrain.Length;
                        Terrain[otherTerrinIdx].Explode(x - m_terrainWidth, y, bulletRadius);
                        audioSource.PlayOneShot(sfxExplosion);
                    }
                }
                bulletCount = count;
                if (bulletCount == 0)
                {
                    SetGameState(GAME_STATE.EXPLOSION);
                    break;
                }
            }

            for (int i = 0; i < bulletCount; i++)
            {
                GameObject bullet = bulletPool[bulletIdx[i]];
                Vector3 position = new Vector3(bulletPos[i].x, bulletPos[i].y, -1.0f);
                bullet.transform.SetPositionAndRotation(position, bullet.transform.rotation);
            }


        }

        void DestroyCastle(GameData gameData)
        {
            gameData.CurrentLevel++;
            gameData.CurrentShots += gameData.ShotsThisLevel;
            gameData.ShotsThisLevel = 0;
            UpdateUI(gameData);

            castle[terrainIdx].SetActive(false);

            Vector2 startPos = castle[terrainIdx].transform.position;
            for (int i = 0; i < debris.Length; i++)
            {
                debrisCheckC[i] = false;
                debrisPos[i] = startPos;
                debrisDir[i] = new Vector2(Random.Range(-2.0f, 2.0f), Random.Range(2.0f, 4.0f));
                debrisRot[i] = Random.Range(3.0f, 6.0f);
                if (Random.value < 0.5f)
                    debrisRot[i] = -debrisRot[i];
                debris[i].SetActive(true);
            }
            debrisCount = debris.Length;

            audioSource.PlayOneShot(sfxCastle);
        }

        void ShowCastle(int tIndex, int level)
        {
            castle[terrainIdx].SetActive(true);

            int castleIndex = 0;

            if (level == 9)
                castleIndex = 1;
            else if (level == 24)
                castleIndex = 2;
            else if (level == 49)
                castleIndex = 3;
            else if (level > 0 && (level + 1) % 100 == 0)
                castleIndex = (int)(((level + 1) / 100 + 3) % castleSprites.Length);

            castle[tIndex].GetComponentInChildren<SpriteRenderer>().sprite = castleSprites[castleIndex];
            castle[tIndex].GetComponentInChildren<TMP_Text>().text = (level + 1).ToString();
        }
    }
}