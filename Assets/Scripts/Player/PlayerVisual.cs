using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class WeaponVisual
{
    public string weaponName;     // 예: "일반 도끼", "고급 도끼"
    public GameObject prefab;     // 무기 프리팹
}

[System.Serializable]
public class ArmorVisual
{
    public string armorName;      // 예: "일반 갑옷", "영웅 갑옷"
    public Color color;           // 방어구 색상
}

public class PlayerVisual : MonoBehaviour
{
    [Header("Player Meshes")]
    public SkinnedMeshRenderer bodyRenderer;   // Paladin_J_Nordstrom
    public SkinnedMeshRenderer helmetRenderer; // Paladin_J_Nordstrom_Helmet

    [Header("Weapon Holder")]
    public Transform weaponHolder;             // mixamorig:RightHand
    private GameObject currentWeapon;

    [Header("Visual Tables")]
    public List<WeaponVisual> weaponVisuals = new List<WeaponVisual>();
    public List<ArmorVisual> armorVisuals = new List<ArmorVisual>();

    [Header("Defaults")]
    public GameObject defaultWeapon;
    public Color defaultArmorColor = Color.white;

    // ===== 방어구 색상 적용 =====
    public void ApplyArmor(string armorName)
    {
        var found = armorVisuals.Find(v => v.armorName == armorName);
        Color colorToApply = found != null ? found.color : defaultArmorColor;

        ApplyColorToRenderer(bodyRenderer, colorToApply);
        ApplyColorToRenderer(helmetRenderer, colorToApply);
    }

    void ApplyColorToRenderer(SkinnedMeshRenderer renderer, Color color)
    {
        if (renderer == null) return;
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color); // URP/Lit 기본 컬러 속성
        renderer.SetPropertyBlock(block);
    }

    // ===== 무기 모델 교체 =====
    public void ApplyWeapon(string weaponName)
    {
        if (weaponHolder == null) return;

        // 이전 무기 제거
        if (currentWeapon != null)
            Destroy(currentWeapon);

        // weaponName == null 또는 등록 안된 무기 → 기본 도끼 복귀
        if (string.IsNullOrEmpty(weaponName))
        {
            if (defaultWeapon != null)
                defaultWeapon.SetActive(true);

            var defaultPoint = defaultWeapon?.transform.Find("AttackPoint");
            if (defaultPoint != null)
                PlayerController.Instance.attackPoint = defaultPoint;
            else
                PlayerController.Instance.attackPoint = null;
            return;
        }

        // 새 무기 장착 시 기본 도끼 비활성화
        if (defaultWeapon != null)
            defaultWeapon.SetActive(false);

        var found = weaponVisuals.Find(v => v.weaponName == weaponName);
        if (found != null && found.prefab != null)
        {
            currentWeapon = Instantiate(found.prefab, weaponHolder);

            // 새 무기에서 attackPoint 자동 연결
            var newPoint = currentWeapon.GetComponentsInChildren<Transform>(true)
                            .FirstOrDefault(t => t.name == "AttackPoint");
            if (newPoint != null)
                PlayerController.Instance.attackPoint = newPoint;
            else
                PlayerController.Instance.attackPoint = null;
        }
    }
}
