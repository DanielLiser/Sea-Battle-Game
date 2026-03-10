using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int myIndex;
    private Button myButtonComponent;
    public int cellIndex;
    public UnityEngine.UI.Button confirmButton; 
    public TMPro.TextMeshProUGUI statusText;
    private Image cellImage;
    private Color colorBeforeHover;
    public void Setup(int index)
    {
        myIndex = index;
        myButtonComponent = GetComponent<Button>();
        myButtonComponent.onClick.RemoveAllListeners();
        myButtonComponent.onClick.AddListener(() => OnCellClicked());
        //GetComponentInChildren<TMPro.TextMeshProUGUI>().text = index.ToString();

        myButtonComponent.onClick.AddListener(OnCellClicked);
        cellImage = GetComponent<Image>();
    }

    private void OnCellClicked()
    {
        Debug.Log("Clicked on cell index: " + myIndex);
        GameManager.Instance.SelectCell(myIndex);


    }
    public void setHighlight(bool isHighlight) { //highlight a cell
        Image bgImage = GetComponent<Image>();
        if (isHighlight)
        {
            bgImage.color = new Color(0f, 0f, 0.3f, 0.5f);
        }
        else {
            bgImage.color = new Color(0f, 0f, 1f, 0.3f);

        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        colorBeforeHover = cellImage.color;
        string currentAbility = GameManager.Instance.selectedAbillity;

        if (currentAbility == "MISSLE")
        {
            cellImage.color = new Color(1f, 0f, 0f, 0.3f);
        }
        //if (currentAbility == "MINE")
        //{
        //    cellImage.color = Color.yellow;
        //}
        if (currentAbility == "NONE" || currentAbility == "MINE")
        {
            if (GameManager.Instance.isValidMove(myIndex))
            {
                cellImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);    
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cellImage.color = colorBeforeHover;

    }
}