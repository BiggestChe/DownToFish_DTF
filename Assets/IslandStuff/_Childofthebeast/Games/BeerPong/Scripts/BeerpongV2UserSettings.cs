
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonVR.PartyGames.Beerpong
{
    public class BeerpongV2UserSettings : UdonSharpBehaviour
    {
        public BeerpongV2 Controller;
        [Header("User Settings")]
        [Tooltip("Default: true\nIf the ball resapwns if it leaves the play area.")]
        [UdonSynced] public bool RespawnOnLeave = true;
        [Tooltip("Default: true\nIf the ball resapwns if it goes in a cup.")]
        [UdonSynced] public bool RespawnOnCup = true;

        [Tooltip("Default: true\nToggle the animation/particle effects when a ball enters a cup.")]
        public bool hasFX = true;
        [Tooltip("Default: 0\nthe height offset of the cup FX.")]
        public float FxOffset;

        [Tooltip("Default: 1\nTime in secconds that it takes for the ball to respawn when it hits a cup.")]
        public float RespawnBall = 1f;
        [Tooltip("Default: 1\nTime in secconds that it takes for the cup to despawn when a ball hits it.")]
        public float RespawnCup = 1f;

        [Tooltip("Mutes the Audio for the cups.")]
        public bool isFXMuted;
        [Tooltip("Mutes the Audio for the ball collision noise.")]
        public bool isBallMuted;

        [Tooltip("Default: false\nToggles if the table is Walkthough or Solid.")]
        public bool isWalkthrough;
        [Tooltip("Default: 17\nThis is what layer is used for the Walkthrough Layer.\nIf you dont know what this means, leave it set to 17.")]
        public int WalkthroughLayer = 17;
        private int _DefaultLayer = -1;

        private bool _RespawnOnLeave;
        private bool _RespawnOnCup;

        public void init()
        {
            _DefaultLayer = Controller.Table.layer;
        }
        public override void OnDeserialization()
        {
            if (RespawnOnLeave != _RespawnOnLeave)
            {
                _RespawnOnLeave = RespawnOnLeave;
                Controller.UpdateMenus();
            }
            if (RespawnOnCup != _RespawnOnCup)
            {
                _RespawnOnCup = RespawnOnCup;
                Controller.UpdateMenus();
            }
        }
        #region RespawnOnCup
        public void Toggle_RespawnOnCup()
        {
            if (RespawnOnCup)
            {
                Set_RespawnOnCup_false();
            } else
            {
                Set_RespawnOnCup_true();
            }
        }
        private void Set_RespawnOnCup_true()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Network_Update_RespawnOnCup_true");
        }
        private void Set_RespawnOnCup_false()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Network_Update_RespawnOnCup_false");
        }
        public void Network_Update_RespawnOnCup_true()
        {
            RespawnOnCup = true;
            _RespawnOnCup = true;
            Controller.UpdateMenus();
        }
        public void Network_Update_RespawnOnCup_false()
        {
            RespawnOnCup = false;
            _RespawnOnCup = false;
            Controller.UpdateMenus();
        }
        #endregion
        #region RespawnOnLeave
        public void Toggle_RespawnOnLeave()
        {
            if (RespawnOnLeave)
            {
                Set_RespawnOnLeave_false();
            } else
            {
                Set_RespawnOnLeave_true();
            }
        }
        private void Set_RespawnOnLeave_true()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Network_Update_RespawnOnLeave_true");
        }
        private void Set_RespawnOnLeave_false()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Network_Update_RespawnOnLeave_false");
        }
        public void Network_Update_RespawnOnLeave_true()
        {
            RespawnOnLeave = true;
            _RespawnOnLeave = true;
            Controller.UpdateMenus();
        }
        public void Network_Update_RespawnOnLeave_false()
        {
            RespawnOnLeave = false;
            _RespawnOnLeave = false;
            Controller.UpdateMenus();
        }
        #endregion
        public void ToggleLoli()
        {
            Controller.LoliStep.SetActive(!Controller.LoliStep.activeSelf);
            Controller.UpdateMenus();
        }
        public void SetLoliHeight(int _menNum ,float _height)
        {
            var _counter = 0;
            foreach (BeerpongV2_menu _menu in Controller.Menus)
            {
                if (_counter != _menNum) _menu.LoliSlider.value = _height;
                _counter++;
            }
            Controller.LoliStep.transform.localPosition = new Vector3(0, _height, 0);
        }
        public void SetVolumeSlider(int _menNum, float _target)
        {
            var _counter = 0;
            foreach (BeerpongV2_menu _menu in Controller.Menus)
            {
                if (_counter != _menNum) _menu.VolumeSlider.value = _target;
                _counter++;
            }
        }
        public void SetBallVolumeSlider(int _menNum, float _target)
        {
            var _counter = 0;
            foreach (BeerpongV2_menu _menu in Controller.Menus)
            {
                if (_counter != _menNum) _menu.BallVolumeSlider.value = _target;
                _counter++;
            }
        }

        public void ToggleWalkthrough()
        {
            isWalkthrough = !isWalkthrough;
            if (isWalkthrough)
            {
                Controller.Table.layer = WalkthroughLayer;
            } else
            {
                Controller.Table.layer = _DefaultLayer;
            }
        }
    }
}