using Asmos.Timers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class MoneyIndicator : MonoBehaviour
{
    [SerializeField] Ease moveXEase;
    [SerializeField] float moveXValue = -100;
    [SerializeField] float fadeTime;
    [SerializeField] AnimationCurve moveCurve;
    [SerializeField] TMP_Text label;
    [SerializeField] Image image;
    const float MOVE_X_TIME = .4f;
    const float MOVE_Y_TIME = .2f;
    Timer fadeTimer, moveYTimer, moveXTimer, livingTimer;
    CanvasGroup canvasGroup;
    float fromPosY, toPosY;
    float posY, posX;
    void Awake()
    {

        canvasGroup = GetComponent<CanvasGroup>();
        fadeTimer = new(fadeTime);
        fadeTimer.OnTimerUpdate += () => canvasGroup.alpha = fadeTimer.GetPercentage();
        fadeTimer.useUpdateAsRewindAction = true;
        fadeTimer.useTimeScale = false;

        moveYTimer = new(MOVE_Y_TIME);
        moveYTimer.OnTimerUpdate += () =>
        {
            posY = Mathf.Lerp(fromPosY, toPosY, moveCurve.Evaluate(moveYTimer.GetPercentage()));
        };
        moveYTimer.useTimeScale = false;


        moveXTimer = new(MOVE_X_TIME);
        moveXTimer.OnTimerUpdate += () =>
        {
            posX = DOVirtual.EasedValue(moveXValue, 0, moveXTimer.GetPercentage(), moveXEase);
        };
        moveXTimer.useTimeScale = false;



        livingTimer = new(1f, () => fadeTimer.Rewind());
        livingTimer.useTimeScale = false;
    }
    public void Play(float initialYPos, float ttl, string text, Sprite sprite)
    {
        transform.localPosition = new(moveXValue, -initialYPos, 0);
        posX = moveXValue;
        posY = -initialYPos;
        image.sprite = sprite;
        label.SetText(text);

        fadeTimer.Play();
        toPosY = transform.localPosition.y;
        livingTimer.endTime = ttl - fadeTimer.endTime;
        livingTimer.ResetPlay();
        moveXTimer.ResetPlay();
    }
    public void MoveDown(float distance)
    {
        fromPosY = transform.localPosition.y;
        toPosY -= distance;
        moveYTimer.ResetPlay();
    }

    void Update()
    {
        transform.localPosition = new(posX, posY, 0);
    }

}
