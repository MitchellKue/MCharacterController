using Kojiko.MCharacterController.Abilities;
using UnityEngine;

public class ExampleWeaponADSConfig : MonoBehaviour
{
    [SerializeField] private Ability_FPV_AimDownSight _aim;

    [SerializeField] private float _hipFOV = 80f;
    [SerializeField] private float _adsFOV = 55f;
    [SerializeField] private float _hipSens = 1f;
    [SerializeField] private float _adsSens = 0.5f;
    [SerializeField] private Vector3 _adsOffset = new Vector3(0.04f, -0.03f, 0.08f);

    public void OnEquip()
    {
        if (_aim == null) return;

        _aim.SetFOVSettings(_hipFOV, _adsFOV);
        _aim.SetSensitivitySettings(_hipSens, _adsSens);
        _aim.SetADSOffset(_adsOffset);
    }
}