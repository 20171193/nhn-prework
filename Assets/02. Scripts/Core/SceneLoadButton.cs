using UnityEngine;

// 버튼 하나에 붙여서 지정한 씬으로 전환한다. 매칭/전투 등 다른 로직과 무관한 단순 씬 전환
// 전용이라 특정 매니저에 얹지 않고 따로 뺐다 - 씬 이름만 인스펙터에서 채우면 된다.
public class SceneLoadButton : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void Load()
    {
        SoundManager.Instance?.PlaySfxUI(SfxId.ClickNormalBTN);
        FadeManager.Instance.FadeAndLoad(sceneName);
    }
}
