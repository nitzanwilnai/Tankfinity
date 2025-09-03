using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TankFinity
{
    public class Game : MonoBehaviour
    {
        public enum MENU_STATE { PRIVACY_POLICY, IN_GAME };
        public MENU_STATE MenuState;

        public GameObject UIPrivacyPolicy;
        public BoardVisual BoardVisual;

        public GameData GameData = new GameData();

        public void SetMenuState(MENU_STATE newMenuState)
        {
            MenuState = newMenuState;

            UIPrivacyPolicy.SetActive(MenuState == MENU_STATE.PRIVACY_POLICY);
        }

        // Start is called before the first frame update
        void Start()
        {
            SetMenuState(MENU_STATE.PRIVACY_POLICY);

            bool privacyShown = true;
#if !UNITY_EDITOR
            GameData.CurrentShots = PlayerPrefs.GetInt("CurrentShots");
            GameData.CurrentLevel = PlayerPrefs.GetInt("CurrentLevel");
            privacyShown = PlayerPrefs.GetInt("PrivacyPolicy") == 1;
            UIPrivacyPolicy.SetActive(!privacyShown);
#endif
            if (privacyShown)
                BoardVisual.StartGame();

            BoardVisual.Init(GameData);
        }

        // Update is called once per frame
        void Update()
        {
            if(MenuState == MENU_STATE.IN_GAME)
            {
                BoardVisual.Tick(GameData, Time.deltaTime);
            }
        }

        public void GoToPrivacyPolicy()
        {
            Application.OpenURL("http://oujx.com/TankFinityPrivacyPolicy.html");
        }

        public void HidePrivacyPolicy()
        {
            PlayerPrefs.SetInt("PrivacyPolicy", 1);
            SetMenuState(MENU_STATE.IN_GAME);
            UIPrivacyPolicy.SetActive(false);
            BoardVisual.StartGame();
        }
    }
}