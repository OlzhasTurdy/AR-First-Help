using UnityEngine;
using UnityEngine.SceneManagement;

public class EmergencyManager : MonoBehaviour
{
    [Header("Экраны помощи (UI Panels)")]
    public GameObject screenCPR;
    public GameObject screenBleeding;
    public GameObject screenChoking;
    public GameObject screenUnconscious;

    [Header("3D Модели")]
    public GameObject modelCPR;
    public GameObject modelBleeding;
    public GameObject modelChoking;
    public GameObject modelUnconscious;

    [Header("Аудио")]
    public AudioSource voiceSource;
    public AudioSource rhythmSource;
    public AudioClip voiceCPR, voiceBleeding, voiceChoking, voiceUnconscious;

    [Header("Настройки звука (UI)")]
    public GameObject iconSoundOn;  // Иконка включенного звука
    public GameObject iconSoundOff; // Иконка перечеркнутого звука
    private bool isMuted = false;   // Состояние звука
    void Start()
    {
        DeactivateEverything();

        // Берем значение сразу в формате Enum
        EmergencyType choice = EmergencyLoader.SelectedEmergency;

        if (choice == EmergencyType.None)
        {
            Debug.LogWarning("Ничего не выбрано, возвращаемся в меню.");
            return;
        }

        ApplyChoice(choice);
    }

    private void ApplyChoice(EmergencyType choice)
    {
        // Теперь switch работает по Enum (это очень быстро и надежно)
        switch (choice)
        {
            case EmergencyType.CPR:
                SetupScene(screenCPR, modelCPR, voiceCPR, true);
                break;
            case EmergencyType.Bleeding:
                SetupScene(screenBleeding, modelBleeding, voiceBleeding, false);
                break;
            case EmergencyType.Choking:
                SetupScene(screenChoking, modelChoking, voiceChoking, false);
                break;
            case EmergencyType.Unconscious:
                SetupScene(screenUnconscious, modelUnconscious, voiceUnconscious, false);
                break;
        }
    }

    private void SetupScene(GameObject panel, GameObject model, AudioClip clip, bool useRhythm)
    {
        if (panel) panel.SetActive(true);
        if (model) model.SetActive(true);
        if (voiceSource && clip)
        {
            voiceSource.clip = clip;
            voiceSource.Play();
        }
        if (rhythmSource)
        {
            if (useRhythm) rhythmSource.Play();
            else rhythmSource.Stop();
        }
    }

    private void DeactivateEverything()
    {
        if (screenCPR) screenCPR.SetActive(false);
        if (screenBleeding) screenBleeding.SetActive(false);
        if (screenChoking) screenChoking.SetActive(false);
        if (screenUnconscious) screenUnconscious.SetActive(false);

        if (modelCPR) modelCPR.SetActive(false);
        if (modelBleeding) modelBleeding.SetActive(false);
        if (modelChoking) modelChoking.SetActive(false);
        if (modelUnconscious) modelUnconscious.SetActive(false);

        if (voiceSource) voiceSource.Stop();
        if (rhythmSource) rhythmSource.Stop();
    }

    public void ExitToMenu()
    {
        EmergencyLoader.SelectedEmergency = EmergencyType.None;
        SceneManager.LoadScene("MenuScene");
    }
    public void ToggleMute()
    {
        isMuted = !isMuted; // Меняем состояние

        // Управляем громкостью источников
        if (voiceSource) voiceSource.mute = isMuted;
        if (rhythmSource) rhythmSource.mute = isMuted;

        // Переключаем иконки
        if (iconSoundOn) iconSoundOn.SetActive(!isMuted);
        if (iconSoundOff) iconSoundOff.SetActive(isMuted);
    }
}