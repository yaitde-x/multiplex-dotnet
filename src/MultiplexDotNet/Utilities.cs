using System;

namespace Multiplex
{
    static class Utilities
    {
        public static void Times(this int count, Action lambda)
        {
            for (int i = 0; i < count; i++)
                lambda();
        }
    }
}