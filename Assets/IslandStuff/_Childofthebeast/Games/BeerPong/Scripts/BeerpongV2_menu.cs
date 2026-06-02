
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace UdonVR.PartyGames.Beerpong
{

    public class BeerpongV2_menu : UdonSharpBehaviour
    {
        [HideInInspector]
        public int MenuNumber = 0;
        public BeerpongV2 Controller;
        public Slider LoliSlider;
        public GameObject HasFX_Fill;
        public GameObject Loli_Fill;
        public GameObject RespawnOnLeave_Fill;
        public GameObject RespawnOnCup_Fill;
        public Slider VolumeSlider;
        public Slider BallVolumeSlider;
        public GameObject MuteFill;
        public GameObject BallMuteFill;
        public GameObject WalkthroughFill;
        public void InitMenu()
        {
            VolumeSlider.value = Controller.FxAudioSource.volume;
            BallVolumeSlider.value = Controller.Players[0].BallAudio.volume;
            UpdateMenu();
        }


        public void UpdateMenu()
        {
            if (Loli_Fill != null) Loli_Fill.SetActive(Controller.LoliStep.activeSelf);
            if (RespawnOnLeave_Fill != null) RespawnOnLeave_Fill.SetActive(Controller.UserSettings.RespawnOnLeave);
            if (RespawnOnCup_Fill != null) RespawnOnCup_Fill.SetActive(Controller.UserSettings.RespawnOnCup);
            if (HasFX_Fill != null) HasFX_Fill.SetActive(Controller.UserSettings.hasFX);
            if (MuteFill != null) MuteFill.SetActive(Controller.UserSettings.isFXMuted);
            if (VolumeSlider != null) VolumeSlider.interactable = !Controller.UserSettings.isFXMuted;
            if (Controller.FxAudioSource != null) Controller.FxAudioSource.mute = Controller.UserSettings.isFXMuted;
            if (LoliSlider != null) LoliSlider.interactable = (Controller.LoliStep.activeSelf);
            if (WalkthroughFill != null) WalkthroughFill.SetActive(Controller.UserSettings.isWalkthrough);

            if (BallMuteFill != null) BallMuteFill.SetActive(Controller.UserSettings.isBallMuted);
            Controller.Players[0].BallAudio.mute = Controller.UserSettings.isBallMuted;
            Controller.Players[1].BallAudio.mute = Controller.UserSettings.isBallMuted;
            if (BallVolumeSlider != null) BallVolumeSlider.interactable = !Controller.UserSettings.isBallMuted;
        }

        public void UpdateLoli()
        {
            Controller.UserSettings.SetLoliHeight(MenuNumber, LoliSlider.value);
        }
        public void UpdateRespawnOnCup()
        {
            Controller.UserSettings.Toggle_RespawnOnCup();
        }
        public void UpdateRespawnOnLeave()
        {
            Controller.UserSettings.Toggle_RespawnOnLeave();
        }
        public void UpdateLoliActive()
        {
            Controller.UserSettings.ToggleLoli();
        }
        public void UpdateHasFX()
        {
            Controller.UserSettings.hasFX = !Controller.UserSettings.hasFX;
            Controller.UpdateMenus();
        }
        public void MuteFX()
        {
            Controller.UserSettings.isFXMuted = !Controller.UserSettings.isFXMuted;
            Controller.UpdateMenus();
        }
        public void MuteBall()
        {
            Controller.UserSettings.isBallMuted = !Controller.UserSettings.isBallMuted;
            Controller.UpdateMenus();
        }
        public void SetVolume()
        {
            Controller.FxAudioSource.volume = VolumeSlider.value;
            Controller.UserSettings.SetVolumeSlider(MenuNumber, VolumeSlider.value);
        }
        public void SetBallVolume()
        {
            Controller.Players[0].BallAudio.volume = BallVolumeSlider.value;
            Controller.Players[1].BallAudio.volume = BallVolumeSlider.value;
            Controller.UserSettings.SetBallVolumeSlider(MenuNumber, BallVolumeSlider.value);
        }
        public void ToggleWalkthough()
        {
            Controller.UserSettings.ToggleWalkthrough();
        }

    }
}