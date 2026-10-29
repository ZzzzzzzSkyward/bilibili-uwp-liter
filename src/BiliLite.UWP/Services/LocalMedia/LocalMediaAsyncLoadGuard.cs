using System;
using System.Threading.Tasks;

namespace BiliLite.Services.LocalMedia
{
    /// <summary>Prevents an asynchronous local track load or render from applying after selection changes.</summary>
    public sealed class LocalMediaAsyncLoadGuard
    {
        private int generation;
        private object owner;

        public int Begin(object currentOwner)
        {
            owner = currentOwner;
            return ++generation;
        }

        public void Invalidate()
        {
            owner = null;
            generation++;
        }

        public bool IsCurrent(int ticket, object expectedOwner)
        {
            return ticket == generation && ReferenceEquals(owner, expectedOwner);
        }

        public bool TryApply(int ticket, object expectedOwner, Action apply)
        {
            if (!IsCurrent(ticket, expectedOwner)) return false;
            apply?.Invoke();
            return true;
        }

        public async Task<bool> LoadAndApplyAsync<T>(int ticket, object expectedOwner, Func<Task<T>> load, Action<T> apply)
        {
            var result = await load();
            return TryApply(ticket, expectedOwner, () => apply(result));
        }
    }
}
