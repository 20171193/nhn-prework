using System;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 무관 영속 싱글턴(GameManager/CursorManager와 같은 패턴 - Bootstrap이 부팅 시 1회 Instantiate).
// 로비 UI(캔버스+이름 입력창+무기/투척무기 MainSlot+그 안의 확장 슬롯 전부)를 자기 자식으로
// 들고 다닌다 - 로비 씬을 나갔다 다시 들어와도 파괴/재생성되지 않고 그대로 유지된다.
// canvasRoot는 활성 씬이 로비 씬일 때만 보이도록 SceneManager.sceneLoaded로 토글한다.
//
// 게임 시작 시 Awake에서 DB를 읽어 확장 슬롯을 한 번만 생성하고 클릭 핸들러도 그때 한 번만
// 연결한다(오브젝트 자체가 다시 만들어지지 않으므로 매 방문마다 재배선할 필요가 없다).
// 여기서 채운 PlayData 값은 MatchmakingManager.FindMatch()가 매칭을 커밋하는 시점에
// PlayerSetupSync.PublishLocal(PlayData.Current)로 상대에게 자동 전달된다 - 이 클래스가
// 직접 발행할 필요는 없다.
public class LobbyPlayerSetup : Singleton<LobbyPlayerSetup>
{
    [SerializeField] private string lobbySceneName = "Lobby_Test";
    [SerializeField] private GameObject canvasRoot; // 로비 UI 전체 - 로비 씬이 아닐 때는 꺼둔다

    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private MainSlot weaponMainSlot;
    [SerializeField] private MainSlot throwableWeaponMainSlot;

    [SerializeField] private SelectableOption weaponOptionPrefab;
    [SerializeField] private SelectableOption throwableOptionPrefab;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this) return; // 중복 인스턴스는 Singleton이 Destroy 처리하므로 아래는 건너뛴다.

        SetupName();
        SetupSelector(WeaponDatabase.Instance.entries, weaponOptionPrefab, weaponMainSlot,
            w => w.icon, PlayData.Current.weaponId, PlayData.SetWeapon);
        SetupSelector(ThrowableWeaponDatabase.Instance.entries, throwableOptionPrefab, throwableWeaponMainSlot,
            t => t.icon, PlayData.Current.throwableWeaponId, PlayData.SetThrowableWeapon);

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyVisibility(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyVisibility(scene.name);

    void ApplyVisibility(string sceneName)
    {
        if (canvasRoot != null) canvasRoot.SetActive(sceneName == lobbySceneName);
    }

    // 이름 기본값 = 포톤이 임의 지정한 닉네임(MatchmakingManager.Awake와 동일한 폴백) - 빈 값 없음.
    void SetupName()
    {
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = $"Player{UnityEngine.Random.Range(1000, 10000)}";

        nameInputField.text = PhotonNetwork.NickName;
        PlayData.SetName(PhotonNetwork.NickName);

        nameInputField.onEndEdit.AddListener(value =>
        {
            if (!string.IsNullOrWhiteSpace(value)) PlayData.SetName(value);
        });
    }

    // DB 항목마다 확장 슬롯(SelectableOption)을 하나씩 만들어 mainSlot.ExpandPanel 아래 채우고,
    // 클릭 시 그 데이터를 mainSlot에 반영 + PlayData에도 반영하도록 연결한다.
    void SetupSelector<T>(List<T> entries, SelectableOption prefab, MainSlot mainSlot,
        Func<T, Sprite> iconOf, int defaultId, Action<int> onSelect) where T : IHasId
    {
        Sprite defaultSprite = null;

        foreach (var entry in entries)
        {
            var sprite = iconOf(entry);
            var option = Instantiate(prefab, mainSlot.ExpandPanel);
            option.Init(entry.Id, sprite);
            option.SetClickHandler(selected =>
            {
                mainSlot.SetSelected(selected.Id, selected.Icon);
                onSelect(selected.Id);
            });

            if (entry.Id == defaultId) defaultSprite = sprite;
        }

        mainSlot.SetSelected(defaultId, defaultSprite);
        onSelect(defaultId); // PlayData에도 즉시 반영 - 빈 선택 없음
    }
}
