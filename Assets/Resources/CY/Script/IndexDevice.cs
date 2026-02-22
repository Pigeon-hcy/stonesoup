using UnityEngine;
using TMPro;
using System.Collections;

public class IndexDevice : Tile
{

    public string[] missionText = {
        "Stand on any three-way intersection at 3:38 tomorrow, look to the east, and wave seven times",
        "Picture yourself in your favorite Association's uniform, let it sink in, and wish a happy new year to a couple walking a dog",
        "For the next 49 hours, look out your window and throw your least valuable possession towards the head of your dreams. ",
        "This evening, spill your blood into the nearest toilet.",
        "Order a risotto and give it to the one you couldn't kill, pretend nothing happened, and hug your nearest coworker",
        "As soon as possible, seek that which will fill your heart.",
        "Pack for a short trip, then steal copper wiring from the house of someone that considers you a friend, but you do not,",
        "Find residents that live in the same building as you, but before you do that, immediately go and knock on your neighbor's door",
        "Rip out the spine of someone who wronged the Index.",
        "For the next 49 hours, run into traffic.",
        "Solve 16 nonogram puzzles in a row.",
        "Exchange the left leg of the fourteenth person you come across today with the right leg of the twenty-sixth person you run into.",
        "Until someone stops you, immigrate to a different district",
        "In 10 hours, convince yourself this is fine and give a love letter to the last weakest person you see before you get home. By then, go to a school."
    };

    public string completeText = ".Clear_";
    public string incompleteText = ".Incom_";

    public AudioClip useSound;
    public AudioClip completeSound;
    public TMP_Text displayText;
    public GameObject enemy;

    public bool isGenerating = false;

    [Header("Mission")]
    public float missionTimeout = 30f;
    public float minDistance = 2f;
    public float maxDistance = 6f;
    public int healthReward = 1;

    protected bool _missionActive = false;
    protected Vector2 _taskDirection;
    protected float _requiredDistance;
    protected Vector2 _taskStartPos;
    public override void useAsItem(Tile tileUsingUs)
    {
        if (isGenerating) return;
        isGenerating = true;
        StartCoroutine(generatePrescriptText());
    }

    public IEnumerator generatePrescriptText()
    {
        string prescript = missionText[Random.Range(0, missionText.Length)];
        if (displayText != null) displayText.text = "";
        AudioManager.playAudio(useSound);
        for (int i = 0; i < prescript.Length; i++)
        {
            displayText.text += prescript[i];
            yield return new WaitForSeconds(0.01f);
        }
        yield return new WaitForSeconds(3f);
        generateMission();
        displayText.text = "";
        isGenerating = false;
    }

    public IEnumerator generateMissionCompleteText()
    {
        string prescript = completeText;
        if (displayText != null) displayText.text = "";
        if (completeSound != null) AudioManager.playAudio(completeSound);
        for (int i = 0; i < prescript.Length; i++)
        {
            displayText.text += prescript[i];
            yield return new WaitForSeconds(0.01f);
        }
        yield return new WaitForSeconds(3f);
        if (displayText != null) displayText.text = "";
        isGenerating = false;
    }

    public IEnumerator generateMissionIncompleteText()
    {
        string prescript = incompleteText;
        if (displayText != null) displayText.text = "";
        AudioManager.playAudio(useSound);
        for (int i = 0; i < prescript.Length; i++)
        {
            displayText.text += prescript[i];
            yield return new WaitForSeconds(0.01f);
        }
        yield return new WaitForSeconds(3f);
        displayText.text = "";
        isGenerating = false;
    }

    public void generateMission()
    {
        if (_tileHoldingUs == null) return;
        _missionActive = true;
        _taskStartPos = _tileHoldingUs.transform.position;
        _requiredDistance = Random.Range(minDistance, maxDistance + 1) * Tile.TILE_SIZE;
        int dirIndex = Random.Range(0, 4);
        _taskDirection = dirIndex switch { 0 => Vector2.up, 1 => Vector2.right, 2 => Vector2.down, _ => Vector2.left };
        Invoke(nameof(onMissionTimeout), missionTimeout);
    }

    protected virtual void Update()
    {
        if (!_missionActive || _tileHoldingUs == null) return;
        Vector2 currentPos = _tileHoldingUs.transform.position;
        float traveled = Vector2.Dot(currentPos - _taskStartPos, _taskDirection);
        if (traveled >= _requiredDistance) completeMission();
    }

    void completeMission()
    {
        if (!_missionActive) return;
        _missionActive = false;
        CancelInvoke(nameof(onMissionTimeout));
        if (_tileHoldingUs != null) _tileHoldingUs.restoreAllHealth();
        StartCoroutine(generateMissionCompleteText());
    }

    void onMissionTimeout()
    {
        if (!_missionActive) return;
        _missionActive = false;
        if (_tileHoldingUs != null && enemy != null)
        {
            Transform room = _tileHoldingUs.transform.parent;
            Vector2 localPos = room.InverseTransformPoint(_tileHoldingUs.transform.position);
            Vector2 grid = Tile.toGridCoord(localPos.x, localPos.y);
            int gx = Mathf.Clamp((int)grid.x + Random.Range(-2, 3), 0, LevelGenerator.ROOM_WIDTH - 1);
            int gy = Mathf.Clamp((int)grid.y + Random.Range(-2, 3), 0, LevelGenerator.ROOM_HEIGHT - 1);
            Tile.spawnTile(enemy, room, gx, gy);
        }
        StartCoroutine(generateMissionIncompleteText());
    }

    void clearDisplayText()
    {
        if (displayText != null) displayText.text = "";
    }

    public override void dropped(Tile tileDroppingUs)
    {
        if (_missionActive)
        {
            _missionActive = false;
            CancelInvoke(nameof(onMissionTimeout));
            CancelInvoke(nameof(clearDisplayText));
            if (displayText != null) displayText.text = "";
        }
        base.dropped(tileDroppingUs);
    }
}
