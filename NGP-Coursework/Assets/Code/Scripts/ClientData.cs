[System.Serializable]
public class ClientData
{
    public ulong ClientID { get; }

    public BuildData BuildData { get; set; }


    private ClientData() { }
    public ClientData(ulong clientID) => ClientID = clientID;
}

public class BuildData
{
    public FrameData ActiveFrame;
    public LegsData ActiveLeg;
    public WeaponData ActivePrimaryWeapon;
    public WeaponData ActiveSecondaryWeapon;
    public WeaponData ActiveTertiaryWeapon;
    public AbilityData ActiveAbility;


    public BuildData(FrameData activeFrame, LegsData activeLeg, WeaponData activePrimaryWeapon, WeaponData activeSecondaryWeapon, WeaponData activeTertiaryWeapon, AbilityData activeAbility)
    {
        SetBuildData(activeFrame, activeLeg, activePrimaryWeapon, activeSecondaryWeapon, activeTertiaryWeapon, activeAbility);
    }
    public BuildData SetBuildData(FrameData activeFrame, LegsData activeLeg, WeaponData activePrimaryWeapon, WeaponData activeSecondaryWeapon, WeaponData activeTertiaryWeapon, AbilityData activeAbility)
    {
        // Set our build data.
        this.ActiveFrame = activeFrame;
        this.ActiveLeg = activeLeg;
        this.ActivePrimaryWeapon = activePrimaryWeapon;
        this.ActiveSecondaryWeapon = activeSecondaryWeapon;
        this.ActiveTertiaryWeapon = activeTertiaryWeapon;
        this.ActiveAbility = activeAbility;

        // Return for fluent interface.
        return this;
    }
}