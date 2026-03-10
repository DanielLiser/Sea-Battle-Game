using System.Collections;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;
    public GameObject cellPrefab; // נגרור לכאן את השבלונה מהתיקייה
    public Transform boardPanel;  // נגרור לכאן את ה-GameBoard
    public int totalCells = 12;    // לוח של 4 על 3
    public RectTransform myShipObj;
    public RectTransform enemyShipObj;
    public Transform[] gridCells = new Transform[12];

    void Start()
    {
        CreateBoard();
        RefreshHighlights();
    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void CreateBoard()
    {
        for (int i = 0; i < totalCells; i++)
        {
            // 1. שכפול השבלונה לתוך פאנל הלוח
            GameObject newCell = Instantiate(cellPrefab, boardPanel);

            // 2. קביעת שם מסודר בהיררכיה (Cell_0, Cell_1...)
            newCell.name = "Cell_" + i;
            gridCells[i] = newCell.transform;
            // 3. משיכת הסקריפט והגדרת האינדקס
            GridButton gridBtn = newCell.GetComponent<GridButton>();
            gridBtn.Setup(i);
        }
        Canvas.ForceUpdateCanvases();
        myShipObj.position = gridCells[10].position;
        enemyShipObj.position = gridCells[1].position;
    }
    public void RefreshHighlights() // Highlight the moveable cells
    {
        for (int i = 0; i < totalCells; i++) {
            GridButton btn = gridCells[i].GetComponent<GridButton>();
            bool valid=GameManager.Instance.isValidMove(i);
            btn.setHighlight(valid);
        }

    }

    public void PlaceMineOnGrid(GameObject mine, int cellIndex)
    {
        if (cellIndex >= 0 && cellIndex < gridCells.Length)
        {
            // 1. הופכים את המוקש ל"בן" של המשבצת (ככה הוא מקבל את המיקום שלה אוטומטית)
            mine.transform.SetParent(gridCells[cellIndex].transform);

            // 2. ניגשים לרכיב ה-UI של המוקש
            RectTransform rt = mine.GetComponent<RectTransform>();
            if (rt != null)
            {
                // זה הקסם: מאפסים את המיקום היחסי. 
                // ברגע שזה (0,0), המוקש חייב להיות בדיוק במרכז המשבצת!
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;

                // איפוס ציר ה-Z כדי שלא ייעלם
                Vector3 lp = rt.localPosition;
                lp.z = 0;
                rt.localPosition = lp;
            }
        }
    }

    [Header("Animation Settings")]
    public float shipMoveSpeed = 0.00002f; // מהירות השיט (ככל שהמספר גבוה יותר, זה מהיר יותר)

    // הפונקציה המעודכנת - עכשיו היא Coroutine של אנימציה!
    // חשוב: שיניתי את סוג ההחזרה ל-IEnumerator!
    public IEnumerator MoveShip(RectTransform ship, int index)
    {
        if (gridCells != null && index >= 0 && index < gridCells.Length && gridCells[index] != null)
        {
            // שימוש ב-position העולמי כדי להתגבר על הבדלי ההיררכיה ב-Canvas
            Vector3 startPos = ship.position;
            Vector3 endPos = gridCells[index].position;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * shipMoveSpeed;

                // תנועה חלקה לפי המיקום העולמי
                ship.position = Vector3.Lerp(startPos, endPos, t);

                yield return null;
            }

            // נחיתה מדויקת במרכז המשבצת בסוף האנימציה
            ship.position = endPos;

            Debug.Log($"Ship {ship.name} arrived exactly at cell {index}!");
        }
        else
        {
            Debug.LogError($"MoveShip Error: Invalid index {index} or missing cell.");
        }
    }
}