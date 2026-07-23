namespace Mockup;

public static class IdGenerator
{
    public static long NewID
    {
        get
        {
            long value = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
            return value < 0 ? -value : value;
        }
    }
}
