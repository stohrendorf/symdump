using System;

namespace symfile.memory
{
    public class UnalignedAccessException : Exception
    {
        public UnalignedAccessException(uint offset, uint alignment)
            : base($"Offset {offset} is not aligned with {alignment}")
        {
        }

        public UnalignedAccessException(uint offset, string message)
            : base($"Offset {offset} is not aligned: {message}")
        {
        }
    }
}