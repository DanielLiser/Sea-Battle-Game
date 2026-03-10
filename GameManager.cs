using System;
using System.Collections;
using System.Diagnostics;

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public TMP_Text statusText;
    public TMP_Text timerText;
    private bool isTimerRunning = false;
    public int myPlayerIndex;
    public float currentTime = 0;
    public int selectedCell = -1;
    public BoardManager board;
    public Button confirmButton;
    public int currentCell = 10;
    public String selectedAbillity="NONE";
    public int abilityTargetCell = -1;
    public Slider myHealthBar;
    public Slider enemyHealthBar;
    public GameObject WonScreen;
    public GameObject LostScreen;
    public GameObject DrawScreen;
    public GameObject minePrefab;
    private GameObject currentVisualGhostMine;
    public int missleAmount = 5;
    public int mineAmount = 3;
    public int shieldAmount = 2;

    [Header("Ammo UI Texts")]
    public TMP_Text missileAmmoText;
    public TMP_Text mineAmmoText;
    public TMP_Text shieldAmmoText;


    [Header("Ability Buttons")]
    public Button missileButton;
    public Button mineButton;
    public Button shieldButton;

    [Header("Shield Animation Settings")]
    public GameObject shieldPrefab; 

    private bool isWaitingForTurnResult = false;


    public void LockUI()
    {
        isWaitingForTurnResult = true;
        if (missileButton != null) missileButton.interactable = false;
        if (mineButton != null) mineButton.interactable = false;
        if (shieldButton != null) shieldButton.interactable = false;
    }

    public void UnlockUI()
    {
        isWaitingForTurnResult = false;

        if (missileButton != null) missileButton.interactable = (missleAmount > 0);
        if (mineButton != null) mineButton.interactable = (mineAmount > 0);
        if (shieldButton != null) shieldButton.interactable = (shieldAmount > 0);
    }



    void Awake()
    {
        Application.runInBackground = true;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateAmmoUI()
    {
        if (missileAmmoText != null) missileAmmoText.text = "MISSLE (" + missleAmount + ")";
        if (mineAmmoText != null) mineAmmoText.text = "MINE (" + mineAmount + ")";
        if (shieldAmmoText != null) shieldAmmoText.text = "SHIELD (" + shieldAmount + ")";
    }
    void Start()
    {
           if(NetworkManager.Instance != null && NetworkManager.Instance.myPlayerIndex!=0)
        {
            myPlayerIndex = NetworkManager.Instance.myPlayerIndex;


            statusText.text = "You are player :" + myPlayerIndex;
        }
        else
        {
            statusText.text = "ERROR- DIDNT RECIVE PLYAER INDEX";
        }
        WonScreen.SetActive(false);
        LostScreen.SetActive(false);
        DrawScreen.SetActive(false);
        UpdateAmmoUI();
        if (myHealthBar != null)
        {
            myHealthBar.maxValue = 3;
            myHealthBar.value = 3;
        }

        if (enemyHealthBar != null)
        {
            enemyHealthBar.maxValue = 3;
            enemyHealthBar.value = 3;
        }

    }

    public void SelectCell(int index)
    {
        if (isWaitingForTurnResult || !isTimerRunning) return;
        if (selectedAbillity == "MISSLE")
        {
            abilityTargetCell = index;
            //statusText.color = Color.yellow;
        }
        else
        {
          
            if (isValidMove(index) || index == currentCell)
            {
                selectedCell = index;
            }
            else
            {
                selectedCell = -1;
            }
        }
        CheckConfirmReady();
    }
    public void CheckConfirmReady()
    {
        confirmButton.interactable = false;

        bool hasValidMove = (selectedCell != -1);

        if (selectedAbillity == "NONE")
        {
            if (hasValidMove && selectedCell != currentCell)
            {
                confirmButton.interactable = true;
            }
        }
        else if (selectedAbillity == "MISSLE")
        {
            if (abilityTargetCell != -1 && missleAmount > 0 && hasValidMove)
            {
                confirmButton.interactable = true;
            }
        }
        else if (selectedAbillity == "MINE")
        {
            if (mineAmount > 0 && hasValidMove)
            {
                confirmButton.interactable = true;
            }
        }
        else if (selectedAbillity == "SHIELD")
        {
            if (shieldAmount > 0 && hasValidMove)
            {
                confirmButton.interactable = true;
            }
        }
    }


    public void SelectAbillity(string abillityName)
    {
        if (isWaitingForTurnResult || !isTimerRunning) return;
        if (currentVisualGhostMine != null) Destroy(currentVisualGhostMine);

        if (selectedAbillity == abillityName)
        {
            selectedAbillity = "NONE";
        }
        else
        {
            selectedAbillity = abillityName;

            if (selectedCell == -1)
            {
                selectedCell = currentCell;
            }

            if (selectedAbillity == "MINE")
            {
                if (minePrefab == null) return;
                currentVisualGhostMine = Instantiate(minePrefab);
                currentVisualGhostMine.transform.SetParent(board.gridCells[currentCell].transform, false);

                Image mineImage = currentVisualGhostMine.GetComponent<Image>();
                if (mineImage != null)
                {
                    Color c = mineImage.color;
                    c.a = 0.5f;
                    mineImage.color = c;
                }

                RectTransform mineRt = currentVisualGhostMine.GetComponent<RectTransform>();
                if (mineRt != null)
                {
                    mineRt.anchoredPosition = Vector2.zero;
                    mineRt.sizeDelta = new Vector2(100, 100);
                    mineRt.localScale = Vector3.one;
                }
                currentVisualGhostMine.transform.SetAsLastSibling();
            }
        }
        CheckConfirmReady();
    }

    public void SendMoveToServer()
    {
        int serverIndex = GetVisualIndex(selectedCell);

        GameData data = new GameData("MOVE", "");
        data.index = serverIndex;
        data.abillity = selectedAbillity;

        if (selectedAbillity == "MISSLE")
        {
            data.abilityTargetCell = GetVisualIndex(abilityTargetCell);
        }

        if (selectedAbillity == "MINE")
        {
            data.abilityTargetCell = GetVisualIndex(currentCell);

            if (currentVisualGhostMine != null)
            {
                currentVisualGhostMine.name = "MyMine_" + currentCell;

                // מחזירים לאטום
                Image mineImage = currentVisualGhostMine.GetComponent<Image>();
                if (mineImage != null)
                {
                    Color c = mineImage.color;
                    c.a = 1f;
                    mineImage.color = c;
                }

                // מוודאים שהוא עדיין מקובע למשבצת (עם false!)
                currentVisualGhostMine.transform.SetParent(board.gridCells[currentCell].transform, false);

                currentVisualGhostMine = null;
            }
        }

        NetworkManager.Instance.SendJson(data);

        confirmButton.interactable = false;
        //selectedCell = -1;
        //selectedAbillity = "NONE";
        //abilityTargetCell = -1;
        //statusText.text = "Waiting for opponent...";
    }
    public bool isValidMove(int index) //decide if a cell is a legit index
    {

        if (currentCell % 3 == 0)// LEFT INDEXES
        {
            if (currentCell - index == -1)
            {
                return true;

            }
            if (Mathf.Abs(currentCell - index) == 3)
            {
                return true;
            }
        }
        else if (currentCell%3==2) { 
            if(currentCell - index == 1)
            {
                return true;
            }
            if (Mathf.Abs(currentCell - index) == 3) {
                return true;
            }
        
        
        }
        else 
        {
            if(Mathf.Abs(currentCell - index) == 3) { 
                return true;
            }
            if (Mathf.Abs(currentCell - index) == 1)
            {
                return true;
            }
        }

            return false;
    }

    public void onConfirmedClicked() {
        confirmButton.interactable = false;
        if (selectedAbillity == "MISSLE") {
            missleAmount--;
        }
        if (selectedAbillity == "MINE")
        {
            mineAmount--;
        }
        if (selectedAbillity == "SHIELD")
        {
            shieldAmount--;
        }
        UpdateAmmoUI();
        LockUI();
        SendMoveToServer();    

    }


    // פונקציה שתקרא כשלוחצים על כפתור "Confirm"
    

    void Update()
    {
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;   
            timerText.text = Mathf.Ceil(currentTime).ToString();
            if (currentTime < 0)
            {
                timerText.text = "0";
                isTimerRunning = false;
            }

        }
        if (NetworkManager.Instance != null && NetworkManager.Instance.lastRecivedData != null)
        {
            GameData data = NetworkManager.Instance.lastRecivedData;
            NetworkManager.Instance.lastRecivedData = null;

            UnityEngine.Debug.Log("GameManager received message type: " + data.type);

            HandleServerMessage(data);
        }
    }
    public int GetVisualIndex(int index)
    {
        // player 2 sees the grid upside down
        if (this.myPlayerIndex == 2)
        {
            return 11 - index;
        }
        
        return index;
    }

    public void backToLobby() {
        SceneManager.LoadScene("Lobby");
    }

    private void HandleServerMessage(GameData data)
    {
        switch (data.type)
        {
            case "ROUND START":
                UnityEngine.Debug.Log("Received round start");

                selectedCell = -1;
                selectedAbillity = "NONE";
                abilityTargetCell = -1;
                if (currentVisualGhostMine != null) Destroy(currentVisualGhostMine);

                currentTime = data.time;
                isTimerRunning = true;

                UnlockUI();
                CheckConfirmReady(); 

                break;



            case "TURN_RESULT":
                isTimerRunning = false;
                LockUI();

                int enemyServerPos = (myPlayerIndex == 1) ? data.p2Move : data.p1Move;
                int myServerPos = (myPlayerIndex == 1) ? data.p1Move : data.p2Move;

                int enemyHp = (myPlayerIndex == 1) ? data.p2_hp : data.p1_hp;
                int myTargetHp = (myPlayerIndex == 1) ? data.p1_hp : data.p2_hp;

                bool iHitMine = (myPlayerIndex == 1) ? data.p1_hit_mine : data.p2_hit_mine;
                bool enemyHitMine = (myPlayerIndex == 1) ? data.p2_hit_mine : data.p1_hit_mine;

                float shipMoveDuration = 1.5f; 

                if (data.collision_event == true)
                {
                    StartCoroutine(DelayedMineExplosion(GetVisualIndex(myServerPos), shipMoveDuration));
                }

                if (iHitMine)
                {
                    StartCoroutine(DelayedMineExplosion(GetVisualIndex(myServerPos), shipMoveDuration));
                }

                if (enemyHitMine)
                {
                    StartCoroutine(DelayedMineExplosion(GetVisualIndex(enemyServerPos), shipMoveDuration));
                }

                if (selectedAbillity == "MISSLE" && abilityTargetCell >= 0 && abilityTargetCell < 12)
                {
                    int hitType = 0;
                    int enemyVisualPos = GetVisualIndex(enemyServerPos);
                    if (abilityTargetCell == enemyVisualPos) hitType = (enemyHp <= 0) ? 2 : 1;
                    LaunchMissile(board.myShipObj, abilityTargetCell, hitType, hitType > 0 ? board.enemyShipObj : null);
                }

                if (myPlayerIndex == 1 && data.enemyAbillity == "MISSLE")
                {
                    int targetVisual = GetVisualIndex(data.enemyAbillityTargetCell);
                    if (targetVisual >= 0 && targetVisual < 12)
                    {
                        int enemyHitType = 0;
                        if (data.enemyAbillityTargetCell == data.p1Move) enemyHitType = (data.p1_hp <= 0) ? 2 : 1;
                        LaunchMissile(board.enemyShipObj, targetVisual, enemyHitType, enemyHitType > 0 ? board.myShipObj : null);
                    }
                }
                else if (myPlayerIndex == 2 && data.abillity == "MISSLE")
                {
                    int targetVisual = GetVisualIndex(data.abilityTargetCell);
                    if (targetVisual >= 0 && targetVisual < 12)
                    {
                        int enemyHitType = 0;
                        if (data.abilityTargetCell == data.p2Move) enemyHitType = (data.p2_hp <= 0) ? 2 : 1;
                        LaunchMissile(board.enemyShipObj, targetVisual, enemyHitType, enemyHitType > 0 ? board.myShipObj : null);
                    }
                }

                if (board != null)
                {
                    if (data.collision_event == true)
                    {
                        int collisionVisualCell = GetVisualIndex(myServerPos);
                        StartCoroutine(AnimateCollisionBounce(collisionVisualCell, shipMoveDuration));
                    }
                    else
                    {

                    
                        if (data.p1Move >= 0)
                        {
                            int p1Visual = GetVisualIndex(data.p1Move);
                            StartCoroutine(board.MoveShip(myPlayerIndex == 1 ? board.myShipObj : board.enemyShipObj, p1Visual));
                            if (myPlayerIndex == 1) currentCell = p1Visual;
                        }

                        if (data.p2Move >= 0)
                        {
                            int p2Visual = GetVisualIndex(data.p2Move);
                            StartCoroutine(board.MoveShip(myPlayerIndex == 2 ? board.myShipObj : board.enemyShipObj, p2Visual));
                            if (myPlayerIndex == 2) currentCell = p2Visual;
                        }
                        board.RefreshHighlights();
                    }
                }

                float totalWaitTime = shipMoveDuration;

                bool missileFired = (selectedAbillity == "MISSLE" ||
                                     (myPlayerIndex == 1 && data.enemyAbillity == "MISSLE") ||
                                     (myPlayerIndex == 2 && data.abillity == "MISSLE"));

                if (missileFired)
                {
                    totalWaitTime = Mathf.Max(totalWaitTime, missileDuration);
                }

                if (iHitMine || enemyHitMine || data.collision_event)
                {
                    totalWaitTime = Mathf.Max(totalWaitTime, shipMoveDuration + 1.0f);
                }

                // 7. אנימציות מגן
                bool iUsedShield = (selectedAbillity == "SHIELD");
                bool enemyUsedShield = (myPlayerIndex == 1) ? (data.enemyAbillity == "SHIELD") : (data.abillity == "SHIELD");

                if (iUsedShield) TriggerShieldVisual(board.myShipObj, totalWaitTime);
                if (enemyUsedShield) TriggerShieldVisual(board.enemyShipObj, totalWaitTime);

                StartCoroutine(UpdateHPAfterAnimation(myTargetHp, enemyHp, totalWaitTime));

                break;
        }
    }
    [Header("Missile Animation Settings")]
    public RectTransform missilePrefab; 
    public RectTransform explosionPrefab; 
    public float missileDuration = 3f; 
    public float missileArcHeight = 400;


    [Header("Impact Animations")]
    public GameObject waterSplashPrefab;   // 0 = פגיעה במים
    public GameObject smallExplosionPrefab;// 1 = פגיעה בספינה
    public GameObject largeExplosionPrefab;// 2 = הטבעה
                                           
    private void TriggerShieldVisual(RectTransform targetShip, float duration)
    {
        if (shieldPrefab == null || targetShip == null) return;

        // 1. יוצרים את המגן
        GameObject shield = Instantiate(shieldPrefab);

        // 2. הופכים אותו לילד של הספינה! ככה שאם הספינה זזה, המגן זז איתה
        shield.transform.SetParent(targetShip, false);

        // 3. מוודאים שהוא ממורכז בדיוק על הספינה
        RectTransform rt = shield.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            // אפשר לשחק עם ה-Scale פה אם המגן גדול/קטן מדי לספינה
            rt.localScale = new Vector3(1.2f, 1.2f, 1f);
        }

        // 4. משמידים את המגן אחרי שהזמן (duration) נגמר
        Destroy(shield, duration);
    }

    // פונקציה שממתינה, ואז מעדכנת חיים ובודקת מי ניצח
    private IEnumerator UpdateHPAfterAnimation(int myNewHp, int enemyNewHp, float delayTime)
    {
        // 1. ממתינים את הזמן הדרוש (זמן מעוף הטיל או זמן תנועת הספינה)
        yield return new WaitForSeconds(delayTime);

        // 2. רק עכשיו מעדכנים את מד החיים!
        myHealthBar.value = myNewHp;
        enemyHealthBar.value = enemyNewHp;

        // 3. ורק עכשיו בודקים אם מישהו מת ומקפיצים מסך סיום
        if (myNewHp <= 0 && enemyNewHp <= 0)
        {
            DrawScreen.SetActive(true);
            NetworkManager.Instance.Disconnect();
        }
        else if (myNewHp <= 0)
        {
            LostScreen.SetActive(true);
            NetworkManager.Instance.Disconnect();
        }
        else if (enemyNewHp <= 0)
        {
            WonScreen.SetActive(true);
            NetworkManager.Instance.Disconnect();
        }
    }
    public void LaunchMissile(RectTransform shooterShip, int targetCell, int hitType, RectTransform targetShip = null)
    {
        StartCoroutine(FireMissileCoroutine(shooterShip, targetCell, hitType, targetShip));
    }

    private IEnumerator FireMissileCoroutine(RectTransform shooterShip, int targetCell, int hitType, RectTransform targetShip)
    {
        RectTransform missile = Instantiate(missilePrefab, board.boardPanel.parent);
        missile.SetAsLastSibling();
        missile.localScale = Vector3.one;

        // 1. שומרים את מיקום ההתחלה והסיום במרחב התלת-מימדי מבלי לגעת ב-Z
        Vector3 startPos = shooterShip.position;
        Vector3 endPos = board.gridCells[targetCell].position;

        float t = 0;
        // מחשבים את גובה הקשת מראש
        float scaledArcHeight = missileArcHeight * board.boardPanel.lossyScale.y;

        while (t < 1)
        {
            t += Time.deltaTime / missileDuration;

            // 1. חישוב המיקום הנוכחי של הטיל
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * scaledArcHeight;

            // 2. הטריק: חישוב המיקום העתידי (היכן נהיה בעוד 5% מהזמן)
            float nextT = Mathf.Min(t + 0.05f, 1f); // מוודאים שלא נעבור את 1
            Vector3 nextPos = Vector3.Lerp(startPos, endPos, nextT);
            nextPos.y += Mathf.Sin(nextT * Mathf.PI) * scaledArcHeight;

            // 3. מפנים את הראש אל הנקודה העתידית! (זה מבטיח תנועה חלקה לגמרי)
            Vector3 dir = nextPos - currentPos;
            if (dir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                // שימוש ב-Lerp לסיבוב כדי למנוע "קפיצות" חדות
                Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90);
                missile.rotation = Quaternion.Lerp(missile.rotation, targetRotation, Time.deltaTime * 15f);
            }

            missile.position = currentPos;
            yield return null;
        }

        // --- בחירת האנימציה הנכונה בסוף המעוף ---
        GameObject prefabToSpawn = waterSplashPrefab;
        if (hitType == 1) prefabToSpawn = smallExplosionPrefab;
        else if (hitType == 2) prefabToSpawn = largeExplosionPrefab;

        if (prefabToSpawn != null)
        {
            // ייצור הפיצוץ והצבתו מול המצלמה
            GameObject exp = Instantiate(prefabToSpawn, board.boardPanel.parent);
            exp.transform.position = endPos;

            Vector3 localPos = exp.transform.localPosition;
            localPos.z = -100f;
            exp.transform.localPosition = localPos;

            exp.transform.localScale = new Vector3(100, 100, 100);
            Destroy(exp, 2f);
        }

        // --- אפקט שריפה לספינה אם היא הוטבעה ---
        if (hitType == 2 && targetShip != null)
        {
            UnityEngine.UI.Image shipImage = targetShip.GetComponent<UnityEngine.UI.Image>();
            if (shipImage != null) shipImage.color = new Color(0.3f, 0.3f, 0.3f);
        }

        Destroy(missile.gameObject);
    }
    private void TriggerMineExplosion(int visualCellIndex)
    {
        if (visualCellIndex >= 0 && visualCellIndex < board.gridCells.Length)
        {
            if (smallExplosionPrefab != null)
            {
                GameObject exp = Instantiate(smallExplosionPrefab, board.boardPanel.parent);
                exp.transform.position = board.gridCells[visualCellIndex].position;
                Vector3 localPos = exp.transform.localPosition;
                localPos.z = -100f;
                exp.transform.localPosition = localPos;
                exp.transform.localScale = new Vector3(100, 100, 100);
                Destroy(exp, 2f);
            }
        }
        else
        {
            UnityEngine.Debug.Log("ניסיון לעשות פיצוץ מוקש מחוץ ללוח נחסם: " + visualCellIndex);
        }
    }
    private IEnumerator DelayedMineExplosion(int visualCellIndex, float delay)
    {
        // מחכים את הזמן שלוקח לספינה לזוז
        yield return new WaitForSeconds(delay);

        // מפעילים את הפיצוץ ומעלימים את המוקש מהמים
        TriggerMineExplosion(visualCellIndex);
        RemoveMineVisually(visualCellIndex);
    }
    private void RemoveMineVisually(int visualCellIndex)
    {
        // מוודאים שהמשבצת חוקית
        if (visualCellIndex >= 0 && visualCellIndex < board.gridCells.Length)
        {
            Transform cell = board.gridCells[visualCellIndex].transform;

            // עוברים על כל הילדים של המשבצת
            foreach (Transform child in cell)
            {
                // אם לאובייקט קוראים MyMine (עם מספר כלשהו אחריו), נשמיד אותו
                if (child.name.StartsWith("MyMine_"))
                {
                    Destroy(child.gameObject);
                    UnityEngine.Debug.Log("Mine visually destroyed at cell: " + visualCellIndex);
                }
            }
        }
    }
    // פונקציה שמייצרת אפקט של "ניגוח" והדיפה אחורה
    private IEnumerator AnimateCollisionBounce(int targetVisualCell, float totalDuration)
    {
        // שומרים את המיקומים המקוריים כדי שנדע לאן לחזור
        Vector3 myOriginalPos = board.myShipObj.position;
        Vector3 enemyOriginalPos = board.enemyShipObj.position;
        Vector3 targetPos = board.gridCells[targetVisualCell].position;

        float halfDuration = totalDuration / 2f;
        float t = 0;

        // חלק 1: הספינות טסות אחת לכיוון השנייה (אבל נעצרות ב-70% מהדרך כדי להיראות כמו התנגשות)
        while (t < 1)
        {
            t += Time.deltaTime / halfDuration;
            board.myShipObj.position = Vector3.Lerp(myOriginalPos, Vector3.Lerp(myOriginalPos, targetPos, 0.7f), t);
            board.enemyShipObj.position = Vector3.Lerp(enemyOriginalPos, Vector3.Lerp(enemyOriginalPos, targetPos, 0.7f), t);
            yield return null;
        }

        // חלק 2: בום! (משתמשים באפקט הפיצוץ הקיים שלנו)
        TriggerMineExplosion(targetVisualCell);

        // חלק 3: הדיפה (Bounce) חזרה למקומות המקוריים
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / halfDuration;
            board.myShipObj.position = Vector3.Lerp(Vector3.Lerp(myOriginalPos, targetPos, 0.7f), myOriginalPos, t);
            board.enemyShipObj.position = Vector3.Lerp(Vector3.Lerp(enemyOriginalPos, targetPos, 0.7f), enemyOriginalPos, t);
            yield return null;
        }

        // מוודאים שהם נוחתים בדיוק במרכז המשבצת המקורית
        board.myShipObj.position = myOriginalPos;
        board.enemyShipObj.position = enemyOriginalPos;
    }

}
