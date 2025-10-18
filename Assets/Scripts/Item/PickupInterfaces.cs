// PickupInterfaces.cs
// EnemyController, GoldPickup, UniqueItemPickup 공용 인터페이스

public interface IGoldPickup
{
    void SetGold(int amount);
}

public interface IUniquePickup
{
    void SetItem(string itemName, int amount);
}
