using UnityEngine;
using UnityEngine.UI;

// 무기/투척무기 카테고리별 "현재 선택" 표시 버튼. 클릭하면 자신에게 딸린 확장 슬롯
// 패널(expandPanel)을 열고 닫는 토글 역할이고, 데이터가 반영되면(SetSelected) 그
// 아이콘을 보여주고 패널을 닫는다.
[RequireComponent(typeof(Button))]
public class MainSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Transform expandPanel; // 기본 비활성 - 이 안에 SelectableOption들이 채워진다

    public Transform ExpandPanel => expandPanel;
    public int SelectedId { get; private set; }

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => expandPanel.gameObject.SetActive(!expandPanel.gameObject.activeSelf));
        expandPanel.gameObject.SetActive(false);
    }

    public void SetSelected(int id, Sprite sprite)
    {
        SelectedId = id;
        if (icon != null) icon.sprite = sprite;
        expandPanel.gameObject.SetActive(false);
    }
}
