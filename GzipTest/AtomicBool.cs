using System.Threading;

namespace GzipTest
{
    public class AtomicBool
    {
        private int state;

        public AtomicBool(bool initialValue) => state = initialValue ? 1 : 0;

        public bool TrySet(bool value)
        {
            var intValue = value ? 1 : 0;
            var oppositeValue = value ? 0 : 1;
            return Interlocked.CompareExchange(ref state, intValue, oppositeValue) == intValue;
        }

        public static implicit operator bool(AtomicBool interlockedBool) => interlockedBool.state == 1;
    }
}