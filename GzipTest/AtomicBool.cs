using System.Threading;

namespace GzipTest
{
    public static class Extensions
    {
        public static int ToInt(this bool value) => value ? 1 : 0;
    }

    public class AtomicBool
    {
        private int state;

        public AtomicBool(bool initialValue) => state = initialValue ? 1 : 0;

        public bool TrySet(bool value)
        {
            var intValue = value.ToInt();
            return Interlocked.CompareExchange(ref state, intValue, (!value).ToInt()) == intValue;
        }

        public int Set(bool value) => Interlocked.Exchange(ref state, value.ToInt());

        public static implicit operator bool(AtomicBool interlockedBool) => interlockedBool.state == 1;
    }
}