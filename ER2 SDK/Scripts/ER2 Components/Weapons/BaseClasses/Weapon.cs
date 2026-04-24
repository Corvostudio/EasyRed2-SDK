using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Weapon : HandheldItem
{
    [Tooltip("Pose type for the TPS animation")]
    public WeaponPose weaponPose = WeaponPose.rifle;
}

    public enum MagazineSocket
    {
        noMagazine = 0,
        m14 = 1,
        ak47 = 2,
        greaseGun = 3,
        Thompson = 4,
        MP40 = 5,
        StenMK2,
        gewehr43,
        type100,
        mp44,
        bar,
        mg42,
        bren,
        FG42,
        Browning30Cal,
        lanchester,
        carbine,
        type99,
        colt1911,
        luger,
        nambu,
        beretta1934,
        mab38,
        type97_at,
        breda_m30,
        Type92,
        Type96aa,
        breda_m38,
        flak38,
        Browning50Cal,
        Hispano20mm,
        BoysATRifle,
        thompson1928,
        ppsh41,
        dp28,
        svt40,
        tt33,
        VYa_23,
        mg151_20,
        Owen,
        MaximMG,
        Walter_P38,
        M60
    }

    public enum WeaponPose
    {
        rifle = 1,
        pistol = 2
    }