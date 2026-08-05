using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// PlayerHUD에 존재하는 증강 UI 버튼
// 1. 비활성화(고르지 않은 경우) : lock 이미지, 상호작용 무시
// 2. 활성화 : 증강 종류에 따른 이미지, 클릭 시 일정시간 툴팁 표시
// todo : 증강 DB나 Dictionary, 풀 필요
public class AugmentButton : MonoBehaviour
{
    // 증강 이미지 : 초기 lock 이미지
    private Image augmentIMG;
    // 버튼 클릭시 on, off
    private Image highligtIMG;
    // 버튼 클릭시 on, off 
    private GameObject tooltipGO;

    // 초기화 : lock 이미지로
    public void Init()
    {
        
    }

    // 매개변수로 증강 정보받아오기(증강 풀 존재 시 id로 가져오기)
    public void SetAugment(AugmentDefinition augment)
    {
        // todo: augmentIMG/설명 등 augment.displayName, augment.description으로 채우기
    }

    // 버튼 클릭 시 
    public void OnClickAugmentButton()
    {
        // todo
        // 기본동작
        //  - on : tooltip/highlightImgae on, 5초 뒤 자동 off
        //  - off : tooltip/highlightImage off
    }

    // 툴팁 이미지를 클릭한 경우 : 툴팁이 켜진 상태에서만 동작
    public void OnClickTooltip()
    {
        // 툴팁 닫기
        // tootip off
    }
}
