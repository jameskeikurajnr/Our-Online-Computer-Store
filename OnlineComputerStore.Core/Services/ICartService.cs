using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface ICartService
    {
        Cart GetCart();
        void Add(int productId, int quantity = 1);
        void Remove(int productId);
        void UpdateQuantity(int productId, int quantity);
        void Clear();

        // Called right after a successful login/register: folds anything added
        // to the guest (session) cart into the account's saved cart, so items
        // picked before logging in aren't lost.
        void MergeGuestCartIntoAccount(int userId);

        // Called on logout: copies the account's saved cart into the session so
        // the now-signed-out browser keeps showing the same items (logging out
        // shouldn't look like the cart emptied), while the saved copy remains
        // the durable source of truth for the next login.
        void SnapshotAccountCartToSession(int userId);
    }
}
