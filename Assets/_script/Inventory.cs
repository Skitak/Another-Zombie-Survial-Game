using UnityEngine;

public class Inventory
{
    const int INVENTORY_SIZE = 2;
    Weapon[] weapons;
    int weaponIndex = 0;
    public Inventory() { weapons = new Weapon[INVENTORY_SIZE]; }
    public Weapon weapon { get => weapons[weaponIndex]; }
    public void NextWeapon() => SetWeaponIndex(++weaponIndex);
    public void PreviousWeapon() => SetWeaponIndex(--weaponIndex);
    public void SetWeaponIndex(int newIndex)
    {
        weaponIndex = newIndex % INVENTORY_SIZE;
        // TODO: EquipWeaponAnimation
    }
    public void GrabWeapon(Weapon weapon)
    {
        int index = weaponIndex;
        for (int i = 0; i < INVENTORY_SIZE; i++)
            if (weapons[i] == null)
                index = i;
        if (weapon != null)
            DropWeapon();
        weapon.Grab();
        SetWeaponIndex(index);
    }
    public void DropWeapon() { }

}
