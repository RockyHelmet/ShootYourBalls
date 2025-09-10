using System.Collections;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class GameManagerTest : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int numberOfGoalsToWin = 3;
    [SerializeField] private int numberOfBalls = 5;

    [Header("Prefabs")]
    [SerializeField] private GameObject planePrefab;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject goalPostPrefab;

    [Header("Camera")]
    [SerializeField] private Camera cam;

    [Header("UI Panels")]
    [SerializeField] private GameObject lookDownCommandPanel;
    [SerializeField] private GameObject spawningBallPanel;
    [SerializeField] private GameObject spawningGoalPostPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    [Header("UI Text & Round Display")]
    [SerializeField] private GameObject pointsTextObject;
    [SerializeField] private GameObject roundNumberPanel;
    [SerializeField] private TextMeshProUGUI roundNumberText;

    [Header("AR Root")]
    [SerializeField] private GameObject _XROriginGameObject;
    [SerializeField] private GameObject GoalPanel;
    [SerializeField] private GameObject LostPanel;

    private GameObject spawnedGoalPost;
    private GameObject spawnedBall;
    private float floorHeight;
    private int points = 0;
    private int currentRound = 1;
    private TextMeshProUGUI pointsText;
    private bool hasGameStarted = false;
    private bool isGameOver = false;
    private float timer = 0f;
    private int totalRounds; // Total rounds is one more than the number of balls
    


    private void Start()
    {
        totalRounds = numberOfBalls;
        pointsText = pointsTextObject.GetComponent<TextMeshProUGUI>();

        //lookDownCommandPanel.SetActive(true);
        spawningBallPanel.SetActive(false);
        spawningGoalPostPanel.SetActive(false);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        pointsTextObject.SetActive(false);
        roundNumberPanel.SetActive(false);

        StartCoroutine(SpawnEverything());
    }

    private void Update()
    {
        pointsText.text = $"    Points: {points}\n    Balls Left: {numberOfBalls}";
        if (isGameOver || spawnedBall == null)
            return;

        // Check if ball scored a goal
        BallManager bm = spawnedBall.GetComponent<BallManager>();
        if (bm != null && !bm.wasVictoryAlreadyConfirmed && bm.hasWon)
        {
            points++;
            StartCoroutine(GoalCelebration());
            bm.wasVictoryAlreadyConfirmed = true;
            Goalie goalie = spawnedGoalPost?.GetNamedChild("Motu")?.GetComponent<Goalie>();
            if (goalie != null)
                goalie.moveSpeed *= 1.5f;

            if (points >= numberOfGoalsToWin)
            {
                isGameOver = true;
                victoryPanel.SetActive(true);
                Destroy(spawnedBall, 1f);
                spawnedBall = null;
                return;
            }
            else
            {
                numberOfBalls--;
                StartCoroutine(SpawnNextBall());
                return;
            }
        }
        // if (bm != null && !bm.wasVictoryAlreadyConfirmed && !bm.hasWon)
        // {
        //     bm.wasVictoryAlreadyConfirmed = true;
        //     StartCoroutine(LostCelebration());
        // }

        // Missed swipe? Trigger next round
        BallSwipeShoot bs = spawnedBall.GetComponent<BallSwipeShoot>();
        if (bs != null && bs.ballWasSwiped && numberOfBalls > 0)
        {
            timer += Time.deltaTime;

            if (timer > 3f)
            {
                numberOfBalls--;
                StartCoroutine(LostCelebration());
                StartCoroutine(SpawnNextBall());
                return;
            }
        }

        // Out of balls and not scored
        if (numberOfBalls <= 0 && bm != null && !bm.hasWon)
        {
            isGameOver = true;
            Destroy(spawnedBall, 2f);
            spawnedBall = null;
            defeatPanel.SetActive(true);
        }
    }

    public void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame() {
        Application.Quit();
    }

    private IEnumerator SpawnEverything()
    {
        yield return new WaitForSeconds(2f);
        Vector3 spawnPos = cam.transform.position - Vector3.up.normalized;

        ARPlaneManager planeMgr = _XROriginGameObject.GetComponent<ARPlaneManager>();
        if (planeMgr != null)
        {
            while (planeMgr.trackables.count == 0)
                yield return new WaitForSeconds(0.5f);

            planeMgr.enabled = false;
            foreach (var plane in planeMgr.trackables)
            {
                spawnPos.y = plane.transform.position.y;
                plane.gameObject.SetActive(false);
            }
        }

        spawnPos = spawnPos - Vector3.up.normalized;
        Instantiate(planePrefab, spawnPos, Quaternion.identity);
        //lookDownCommandPanel.SetActive(false);
        spawningBallPanel.SetActive(true);

        floorHeight = spawnPos.y;
        yield return new WaitForSeconds(1f);

        spawnPos = cam.transform.position + cam.transform.forward * 2f;
        spawnPos.y = floorHeight + 2f;
        spawnedBall = Instantiate(ballPrefab, spawnPos, Quaternion.identity);

        spawningBallPanel.SetActive(false);
        spawningGoalPostPanel.SetActive(true);

        yield return new WaitForSeconds(1f);

        spawnPos = cam.transform.position + cam.transform.forward * 10f;
        spawnPos.y = floorHeight;
        spawnedGoalPost = Instantiate(goalPostPrefab, spawnPos, Quaternion.identity);
        spawnedGoalPost.transform.forward = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z);

        spawningGoalPostPanel.SetActive(false);
        pointsTextObject.SetActive(true);
        hasGameStarted = true;

        yield return PlayRoundNumberAnimation(currentRound);
    }

    private IEnumerator SpawnNextBall()
    {
        timer = 0f;
        if (isGameOver) yield break;

        yield return new WaitForSeconds(2f);
        Destroy(spawnedBall);
        spawnedBall = null;
        yield return new WaitForSeconds(2f);

        Vector3 spawnPos = cam.transform.position + cam.transform.forward * 2f;
        spawnPos.y = floorHeight + 2f;
        spawnedBall = Instantiate(ballPrefab, spawnPos, Quaternion.identity);

        currentRound++;
        if (currentRound < totalRounds)
        {
            yield return PlayRoundNumberAnimation(currentRound);
        }
    }

    private IEnumerator PlayRoundNumberAnimation(int round)
    {
        if (isGameOver) yield break;

        roundNumberText.text = "Round " + round;
        roundNumberPanel.SetActive(true);

        CanvasGroup cg = roundNumberPanel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = roundNumberPanel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            cg.alpha = t;
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * 2f;
            cg.alpha = t;
            yield return null;
        }

        roundNumberPanel.SetActive(false);
    }
    IEnumerator GoalCelebration()
    {
        while (false)
        {
            // For the love of Game Development, this is not a parody of a reality show. This is an actual game.
        }


        //BallManager bm = spawnedBall.GetComponent<BallManager>();
        //if (bm.hasWon && !bm.wasVictoryAlreadyConfirmed)
        //{
        //    //wasVictoryAlreadyConfirmed = true;
        //    GoalPanel.SetActive(true);
        //    yield return new WaitForSeconds(2f);
        //    GoalPanel.SetActive(false);
        //}

        yield return null;
    }
    IEnumerator LostCelebration()
    {
        while (false)
        {
            // For the love of Game Development, do not spam this shitty animation and noise into anybody ever again
        }


        //BallManager bm = spawnedBall.GetComponent<BallManager>();
        //if (!bm.hasWon && !bm.wasVictoryAlreadyConfirmed)
        //{
        //    //wasVictoryAlreadyConfirmed = true;
        //    LostPanel.SetActive(true);
        //    yield return new WaitForSeconds(2f);
        //    LostPanel.SetActive(false);
        //}

        yield return null;
    }
}
