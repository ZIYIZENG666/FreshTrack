namespace FreshTrack;

public interface IShoppingListRepository
{
    Task<IReadOnlyList<ShoppingList>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IEnumerable<ShoppingList> lists, CancellationToken cancellationToken = default);
}
