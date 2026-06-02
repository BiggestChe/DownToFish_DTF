using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using VRC.Udon.Common.Interfaces;

namespace UdonVR.PartyGames.Beerpong
{
    public class BeerpongV2 : UdonSharpBehaviour
    {
        [Header("Script Links")]
        public BeerpongV2UserSettings UserSettings;
        public BeerpongV2_menu[] Menus;
        [Header("Table Objects")]
        public BeerpongV2Ball[] Players;
        public Collider[] CupsP1;
        public Collider[] CupsP2;
        [HideInInspector]
        public bool[] CupsP2States;
        [HideInInspector]
        public bool[] CupsP1States;

        public GameObject Table;

        public GameObject Fx;
        public AudioClip FxAudio;
        public ParticleSystem FxPaticle;
        public GameObject FxToggle;
        public float FxToggleDeley = 1f;
        public AudioSource FxAudioSource;
        public AudioClip[] BallSounds;

        [Space(2)]
        public Collider RespawnBox;
        [Space(2)]
        public GameObject LoliStep;
        private float _LoliStep_Max = -0.5f;
        private float _LoliStep_Min = -1f;
        private float _LoliStep_Default = -0.75f;

        private NetworkEventTarget _localPlayer = NetworkEventTarget.All;

        void Start()
        {
            UserSettings.init();
            CupsP1States = new bool[CupsP1.Length];
            CupsP2States = new bool[CupsP2.Length];
            var _counter = 0;
            foreach (bool _bool in CupsP1States)
            {
                CupsP1States[_counter] = true;
                _counter++;
            }
            _counter = 0;
            foreach (bool _bool in CupsP2States)
            {
                CupsP2States[_counter] = true;
                _counter++;
            }
            _counter = 0;
            foreach (BeerpongV2_menu _menu in Menus)
            {
                _menu.MenuNumber = _counter;
                if (_menu.LoliSlider != null)
                {
                    _menu.LoliSlider.maxValue = _LoliStep_Max;
                    _menu.LoliSlider.minValue = _LoliStep_Min;
                    _menu.LoliSlider.value = _LoliStep_Default;
                }
                _counter++;
            }
            if (FxAudio != null && FxAudioSource != null)
            {
                FxAudioSource.clip = FxAudio;
            }
            Players[1].BallAudio.volume = Players[0].BallAudio.volume;
            foreach (BeerpongV2Ball _ball in Players)
            {
                _ball.InitBall();
            }
            foreach (BeerpongV2_menu _menu in Menus)
            {
                _menu.InitMenu();
            }
        }

        public void CheckCollision(int _player, GameObject _ball, Collider _Collider)
        {
            if (!Networking.IsOwner(_ball)) return;
            var _counter = 0;
            if (_player == 0)
            {
                foreach (Collider _col in CupsP1)
                {
                    if (_col == _Collider && CupsP1States[_counter] == true)
                    {
                        SendCustomEvent(("DespawnCup_" + _counter + "_P1"));
                        if (UserSettings.RespawnOnCup) Players[0].N_RespawnBall();
                    }
                    _counter++;
                }
            }
            else if (_player == 1)
            {
                foreach (Collider _col in CupsP2)
                {
                    if (_col == _Collider && CupsP2States[_counter] == true)
                    {
                        SendCustomEvent(("DespawnCup_" + _counter + "_P2"));
                        if (UserSettings.RespawnOnCup) Players[1].N_RespawnBall();
                    }
                    _counter++;
                }
            }
        }

        public void DoFX(Transform _trans)
        {
            if (!UserSettings.hasFX) return;
            if (Fx == null) return;
            Fx.transform.position = _trans.position;
            if (FxAudioSource != null){
                Debug.Log("Playing Audio");
                FxAudioSource.Stop();
                FxAudioSource.Play();
            }
            if (FxPaticle != null)
            {
                Debug.Log("Playing Paticle");
                FxPaticle.Stop();
                FxPaticle.Play();
            }
            if (FxToggle != null)
            {
                Debug.Log("Playing Toggle");
                FxToggle.SetActive(false);
                FxToggle.SetActive(true);
                SendCustomEventDelayedSeconds("DespawnFxToggle", FxToggleDeley);
            }
        }

        public void DespawnFxToggle()
        {
            FxToggle.SetActive(false);
        }

        public void RespawnCups1()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkRepawnCups1");
        }
        public void NetworkRepawnCups1()
        {
            var _counter = 0;
            foreach (Collider _col in CupsP1)
            {
                _col.gameObject.SetActive(true);
                CupsP1States[_counter] = true;
                _counter++;
            }
        }

        public void RespawnCups2()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkRespawnCups2");
        }
        public void NetworkRespawnCups2()
        {
            var _counter = 0;
            foreach (Collider _col in CupsP2)
            {
                _col.gameObject.SetActive(true);
                CupsP2States[_counter] = true;
                _counter++;
            }
        }

        public void UpdateMenus()
        {
            foreach (BeerpongV2_menu _menu in Menus)
            {
                _menu.UpdateMenu();
            }
        }

        #region CupDespawns

        #region Events
        public void DespawnCup_0_P1()
        {
            CupsP1States[0] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_0_P1");
        }
        public void DespawnCup_1_P1()
        {
            CupsP1States[1] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_1_P1");
        }
        public void DespawnCup_2_P1()
        {
            CupsP1States[2] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_2_P1");
        }
        public void DespawnCup_3_P1()
        {
            CupsP1States[3] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_3_P1");
        }
        public void DespawnCup_4_P1()
        {
            CupsP1States[4] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_4_P1");
        }
        public void DespawnCup_5_P1()
        {
            CupsP1States[5] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_5_P1");
        }
        public void DespawnCup_6_P1()
        {
            CupsP1States[6] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_6_P1");
        }
        public void DespawnCup_7_P1()
        {
            CupsP1States[7] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_7_P1");
        }
        public void DespawnCup_8_P1()
        {
            CupsP1States[8] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_8_P1");
        }
        public void DespawnCup_9_P1()
        {
            CupsP1States[9] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_9_P1");
        }

        public void DespawnCup_0_P2()
        {
            CupsP2States[0] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_0_P2");
        }
        public void DespawnCup_1_P2()
        {
            CupsP2States[1] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_1_P2");
        }
        public void DespawnCup_2_P2()
        {
            CupsP2States[2] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_2_P2");
        }
        public void DespawnCup_3_P2()
        {
            CupsP2States[3] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_3_P2");
        }
        public void DespawnCup_4_P2()
        {
            CupsP2States[4] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_4_P2");
        }
        public void DespawnCup_5_P2()
        {
            CupsP2States[5] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_5_P2");
        }
        public void DespawnCup_6_P2()
        {
            CupsP2States[6] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_6_P2");
        }
        public void DespawnCup_7_P2()
        {
            CupsP2States[7] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_7_P2");
        }
        public void DespawnCup_8_P2()
        {
            CupsP2States[8] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_8_P2");
        }
        public void DespawnCup_9_P2()
        {
            CupsP2States[9] = false;
            SendCustomNetworkEvent(_localPlayer, "N_DespawnCup_9_P2");
        }

        #endregion
        //Networked
        #region Networked
        public void N_DespawnCup_0_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[0].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_0_P1", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_1_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[1].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_1_P1", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_2_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[2].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_2_P1", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_3_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[3].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_3_P1", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_4_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[4].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_4_P1", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_5_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[5].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_5_P1", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_6_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[6].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_6_P1", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_7_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[7].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_7_P1", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_8_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[8].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_8_P1", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_9_P1()
        {
            if (UserSettings.hasFX) DoFX(CupsP1[9].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_9_P1", UserSettings.RespawnCup);
        }

        public void N_DespawnCup_0_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[0].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_0_P2", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_1_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[1].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_1_P2", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_2_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[2].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_2_P2", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_3_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[3].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_3_P2", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_4_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[4].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_4_P2", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_5_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[5].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_5_P2", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_6_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[6].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_6_P2", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_7_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[7].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_7_P2", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_8_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[8].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_8_P2", UserSettings.RespawnCup);
        }
        public void N_DespawnCup_9_P2()
        {
            if (UserSettings.hasFX) DoFX(CupsP2[9].transform);
            SendCustomEventDelayedSeconds("Do_DespawnCup_9_P2", UserSettings.RespawnCup);
        }
        #endregion
        public void Do_DespawnCup_0_P1()
        {
            CupsP1[0].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_1_P1()
        {
            CupsP1[1].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_2_P1()
        {
            CupsP1[2].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_3_P1()
        {
            CupsP1[3].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_4_P1()
        {
            CupsP1[4].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_5_P1()
        {
            CupsP1[5].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_6_P1()
        {
            CupsP1[6].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_7_P1()
        {
            CupsP1[7].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_8_P1()
        {
            CupsP1[8].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_9_P1()
        {
            CupsP1[9].gameObject.SetActive(false);
        }

        public void Do_DespawnCup_0_P2()
        {
            CupsP2[0].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_1_P2()
        {
            CupsP2[1].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_2_P2()
        {
            CupsP2[2].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_3_P2()
        {
            CupsP2[3].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_4_P2()
        {
            CupsP2[4].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_5_P2()
        {
            CupsP2[5].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_6_P2()
        {
            CupsP2[6].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_7_P2()
        {
            CupsP2[7].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_8_P2()
        {
            CupsP2[8].gameObject.SetActive(false);
        }
        public void Do_DespawnCup_9_P2()
        {
            CupsP2[9].gameObject.SetActive(false);
        }

        #endregion

    }
}