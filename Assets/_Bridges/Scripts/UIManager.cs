using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;
using SgLib;

// #if EASY_MOBILE
// using EasyMobile;
// #endif

public class UIManager : MonoBehaviour
{
    private GameEvent currentEvent;
    public static bool firstLoad = true;
    private bool mainMenuactive = true;

    [Header("Object References")]
    public GameManager gameManager;
    public CameraController camController;
    public DailyRewardController dailyRewardController;
    public AdDisplayer Ads;

    public GameObject mainCanvas;

    public GameObject settingsUI;
    public GameObject storeUI;
    public GameObject header;
    public Text score;
    public Text bestScore;
    public Text gold;
    public Text title;
    public GameObject tapToStart;
    public GameObject tapToContinue;
    public GameObject characterSelectBtn;
    public GameObject menuButtons;
    public GameObject mainMenuBtn;
    public GameObject removeAds;
    public GameObject continueLostGame;
    public GameObject continueByCoinsBtn;
    public Text dailyRewardBtnText;
    public GameObject dailyRewardBtn;
    public GameObject rewardUI;
    public GameObject soundOffBtn;
    public GameObject soundOnBtn;
    public GameObject musicOnBtn;
    public GameObject musicOffBtn;
    public GameObject restart;

    [Header("Premium Features Only")]
    public GameObject continueByAdsBtn;
    public GameObject watchForCoinsBtn;
    public GameObject iapPurchaseBtn;
    public GameObject removeAdsBtn;
    public GameObject restorePurchaseBtn;


    Animator scoreAnimator;
    Animator dailyRewardAnimator;
    bool isWatchAdsForCoinBtnActive;

    void OnEnable()
    {
        GameManager.NewGameEvent += GameManager_NewGameEvent;
        ScoreManager.ScoreUpdated += OnScoreUpdated;
    }

    void OnDisable()
    {
        GameManager.NewGameEvent -= GameManager_NewGameEvent;
        ScoreManager.ScoreUpdated -= OnScoreUpdated;
    }

    // Use this for initialization
    void Start()
    {
        scoreAnimator = score.GetComponent<Animator>();
        dailyRewardAnimator = dailyRewardBtn.GetComponent<Animator>();

        Reset();
        ShowStartUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (mainCanvas.activeSelf)
        {
            score.text = ScoreManager.Instance.Score.ToString();
            bestScore.text = ScoreManager.Instance.HighScore.ToString();
            gold.text = CoinManager.Instance.Coins.ToString();

            if (!DailyRewardController.Instance.disable && dailyRewardBtn.gameObject.activeSelf)
            {
                TimeSpan timeToReward = DailyRewardController.Instance.TimeUntilReward;

                if (timeToReward <= TimeSpan.Zero)
                {
                    dailyRewardBtnText.text = "GRAB YOUR REWARD!";
                    dailyRewardAnimator.SetTrigger("activate");
                }
                else
                {
                    dailyRewardBtnText.text = string.Format("REWARD IN {0:00}:{1:00}:{2:00}", timeToReward.Hours, timeToReward.Minutes, timeToReward.Seconds);
                    dailyRewardAnimator.SetTrigger("deactivate");
                }
            }
        }

        if (settingsUI.activeSelf)
        {
            UpdateMuteButtons();
            UpdateMusicButtons();
        }
    }

    void GameManager_NewGameEvent(GameEvent e)
    {
        if (e == GameEvent.Start)
        {
            ShowGameUI();
        }
        else if (e == GameEvent.PreGameOver)
        {
            // Before game over, i.e. game potentially will be recovered
        }
        else if (e == GameEvent.GameOver)
        {
            Invoke("ShowGameOverUI", 0.5f);
        }
    }

    void OnScoreUpdated(int newScore)
    {
        scoreAnimator.Play("NewScore");
    }

    void ShowWatchForCoinsBtn()
    {
        // Only show "watch for coins button" if a rewarded ad is loaded and premium features are enabled
        if (IsPremiumFeaturesEnabled() && Ads.CanShowRewardedAd() && Ads.watchAdToEarnCoins)
        {
            watchForCoinsBtn.SetActive(true);
            watchForCoinsBtn.GetComponent<Animator>().SetTrigger("activate");
        }
        else
        {
            watchForCoinsBtn.SetActive(false);
        }
    }

    void ShowDailyRewardBtn()
    {
        // Not showing the daily reward button if the feature is disabled
        if (!DailyRewardController.Instance.disable)
        {
            dailyRewardBtn.SetActive(true);
        }
    }

    void Reset()
    {
        mainCanvas.SetActive(true);
        header.SetActive(false);
        title.gameObject.SetActive(false);
        score.gameObject.SetActive(false);
        tapToStart.SetActive(false);
        tapToContinue.SetActive(false);
        mainMenuBtn.SetActive(false);
        menuButtons.SetActive(false);
        dailyRewardBtn.SetActive(false);
        continueLostGame.SetActive(false);
        continueByCoinsBtn.SetActive(false);
        settingsUI.SetActive(false);

        // Enable or disable premium stuff
        bool enablePremium = PremiumFeaturesManager.Instance.enablePremiumFeatures;
        iapPurchaseBtn.SetActive(enablePremium);
        removeAdsBtn.SetActive(enablePremium);
        restorePurchaseBtn.SetActive(enablePremium);

        // These premium feature buttons are hidden by default
        // and shown when certain criteria are met (e.g. rewarded ad is loaded)
        removeAds.SetActive(false);
        continueByAdsBtn.SetActive(false);
        watchForCoinsBtn.gameObject.SetActive(false);
    }

    public void ShowStartUI()
    {
        mainCanvas.SetActive(true);
        settingsUI.SetActive(false);

        header.SetActive(true);
        title.gameObject.SetActive(true);
        tapToStart.SetActive(true);
        mainMenuBtn.SetActive(true);
        restart.SetActive(false);
        ShowDailyRewardBtn();

        if (GameManager.GameCount == 0)
        {
            ShowWatchForCoinsBtn();
        }
        else if (IsPremiumFeaturesEnabled())
        {
            ShowWatchForCoinsBtn();
        }

        if (!Ads.IsAdRemoved())
        {
            removeAds.SetActive(true);
        }
    }

    public void ShowGameUI()
    {
        header.SetActive(true);
        title.gameObject.SetActive(false);
        score.gameObject.SetActive(true);
        tapToStart.SetActive(false);
        tapToContinue.SetActive(false);
        removeAds.SetActive(false);
        watchForCoinsBtn.SetActive(false);
        mainMenuBtn.SetActive(false);
        dailyRewardBtn.SetActive(false);
        restart.SetActive(false);
    }

    public void ShowContinueLostGameUI(bool canUseCoins, bool canWatchAd)
    {
        continueByCoinsBtn.SetActive(canUseCoins);
        continueByAdsBtn.SetActive(canWatchAd);

        continueLostGame.SetActive(true);
    }

    public void ShowResumeUI()
    {
        tapToContinue.SetActive(true);
        score.gameObject.SetActive(true);
    }

    public void ShowGameOverUI()
    {
        header.SetActive(true);
        title.gameObject.SetActive(false);
        score.gameObject.SetActive(true);
        tapToStart.SetActive(false);
        menuButtons.SetActive(true);
        restart.SetActive(true);

        continueLostGame.SetActive(false);
        watchForCoinsBtn.gameObject.SetActive(false);
        settingsUI.SetActive(false);
        ShowDailyRewardBtn();

        if (IsPremiumFeaturesEnabled())
        {
            ShowWatchForCoinsBtn();
        }

        if (!Ads.IsAdRemoved())
        {
            removeAds.SetActive(true);
        }
    }

    public void ShowSettingsUI()
    {
        header.SetActive(false);
        title.gameObject.SetActive(false);
        score.gameObject.SetActive(false);
        tapToStart.SetActive(false);
        tapToContinue.SetActive(false);
        removeAds.SetActive(false);
        watchForCoinsBtn.SetActive(false);
        mainMenuBtn.SetActive(false);
        menuButtons.SetActive(false);
        dailyRewardBtn.SetActive(false);
        settingsUI.SetActive(true);
        restart.SetActive(false);
    }

    public void HideSettingsUI()
    {
        if (currentEvent == GameEvent.Start)
        {
            ShowStartUI();
        }
        else if (currentEvent == GameEvent.GameOver)
        {
            ShowGameOverUI();
        }
        settingsUI.SetActive(false);
    }

    public void ShowStoreUI()
    {
        header.SetActive(false);
        title.gameObject.SetActive(false);
        score.gameObject.SetActive(false);
        tapToStart.SetActive(false);
        tapToContinue.SetActive(false);
        removeAds.SetActive(false);
        watchForCoinsBtn.SetActive(false);
        mainMenuBtn.SetActive(false);
        menuButtons.SetActive(false);
        dailyRewardBtn.SetActive(false);
        storeUI.SetActive(true);
    }

    public void HideStoreUI()
    {
        if (currentEvent == GameEvent.Start)
        {
            ShowStartUI();
        }
        else if (currentEvent == GameEvent.GameOver)
        {
            ShowGameOverUI();
        }
        storeUI.SetActive(false);
    }

    public void StartGame()
    {
        if (mainMenuactive)
            gameManager.StartGame();
    }

    public void ResumeLostGameByCoins()
    {
        ResumeLostGame(true);
    }

    public void ResumeLostGameByAds()
    {
        AdDisplayer.CompleteRewardedAdToRecoverLostGame += OnCompleteRewardedAdToRecoverLostGame;
        Ads.ShowRewardedAdToRecoverLostGame();
    }

    void OnCompleteRewardedAdToRecoverLostGame()
    {
        // Unsubscribe
        AdDisplayer.CompleteRewardedAdToRecoverLostGame -= OnCompleteRewardedAdToRecoverLostGame;

        // Resume game
        ResumeLostGame(false);
    }

    public void ResumeLostGame(bool useCoins)
    {
        continueLostGame.SetActive(false);
        ShowResumeUI();
        gameManager.RecoverLostGame(useCoins);
    }

    public void EndGame()
    {
        gameManager.GameOver();
    }

    public void RestartGame()
    {
        if (mainMenuactive)
        {
            if (Ads.adsReady)
            {
                StartCoroutine(ShowInterstitial(Ads.showInterstitialDelay));
            }
            else
            {
                gameManager.RestartGame(0.2f);
            }
        }
    }

    IEnumerator ShowInterstitial(float delay = 0f)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        Ads.ShowInterstitialAd();
    }

    public void Restart()
    {
        gameManager.RestartGame(0.2f);
    }

    public void WatchRewardedAdForCoins()
    {
        // Hide the button
        watchForCoinsBtn.SetActive(false);

        AdDisplayer.CompleteRewardedAdToEarnCoins += OnCompleteRewardedAdToEarnCoins;
        Ads.ShowRewardedAdToEarnCoins();
    }

    void OnCompleteRewardedAdToEarnCoins()
    {
        // Unsubscribe
        AdDisplayer.CompleteRewardedAdToEarnCoins -= OnCompleteRewardedAdToEarnCoins;

        // Give the coins!
        ShowRewardUI(Ads.rewardedCoins);

    }

    public void GrabDailyReward()
    {
        if (DailyRewardController.Instance.TimeUntilReward <= TimeSpan.Zero)
        {
            float reward = UnityEngine.Random.Range(dailyRewardController.minRewardValue, dailyRewardController.maxRewardValue);

            // Round the number and make it mutiplies of 5 only.
            int roundedReward = (Mathf.RoundToInt(reward) / 5) * 5;

            // Show the reward UI
            ShowRewardUI(roundedReward);

            // Update next time for the reward
            DailyRewardController.Instance.SetNextRewardTime(dailyRewardController.rewardIntervalHours, dailyRewardController.rewardIntervalMinutes, dailyRewardController.rewardIntervalSeconds);
        }
    }

    public void ShowRewardUI(int reward)
    {
        if (mainMenuBtn.activeSelf)
        {
            mainMenuBtn.SetActive(false);
            mainMenuactive = false;
        }
        rewardUI.SetActive(true);
        rewardUI.GetComponent<RewardUIController>().Reward(reward);
    }

    public void HideRewardUI()
    {
        if (!mainMenuactive)
        {
            mainMenuBtn.SetActive(true);
            mainMenuactive = true;
        }
    }

    // public void ShowLeaderboardUI()
    // {
    //     #if EASY_MOBILE
    //             if (GameServices.IsInitialized())
    //             {
    //                 GameServices.ShowLeaderboardUI();
    //             }
    //             else
    //             {
    //     #if UNITY_IOS
    //                 NativeUI.Alert("Service Unavailable", "The user is not logged in to Game Center.");
    //     #elif UNITY_ANDROID
    //                 GameServices.Init();
    //     #endif
    //             }
    //     #endif
    // }

    [Obsolete]
    public void PurchaseRemoveAds()
    {
#if EASY_MOBILE
        InAppPurchaser.Instance.Purchase(InAppPurchaser.Instance.removeAds);
#endif
    }

    public void RestorePurchase()
    {
#if EASY_MOBILE
        InAppPurchaser.Instance.RestorePurchase();
#endif
    }

    public void ToggleSound()
    {
        SoundManager.Instance.ToggleMute();
    }

    public void ToggleMusic()
    {
        SoundManager.Instance.ToggleMusic();
    }

    public void SelectCharacter()
    {
        SceneManager.LoadScene("CharacterSelection");
    }

    void UpdateMuteButtons()
    {
        if (SoundManager.Instance.IsMuted())
        {
            soundOnBtn.gameObject.SetActive(false);
            soundOffBtn.gameObject.SetActive(true);
        }
        else
        {
            soundOnBtn.gameObject.SetActive(true);
            soundOffBtn.gameObject.SetActive(false);
        }
    }

    void UpdateMusicButtons()
    {
        if (SoundManager.Instance.IsMusicOff())
        {
            musicOffBtn.gameObject.SetActive(true);
            musicOnBtn.gameObject.SetActive(false);
        }
        else
        {
            musicOffBtn.gameObject.SetActive(false);
            musicOnBtn.gameObject.SetActive(true);
        }
    }

    bool IsPremiumFeaturesEnabled()
    {
        return PremiumFeaturesManager.Instance != null && PremiumFeaturesManager.Instance.enablePremiumFeatures;
    }
}
