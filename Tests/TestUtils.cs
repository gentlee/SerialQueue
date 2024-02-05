namespace Tests
{
    public static class TestUtils
    {
        public static Task RandomDelay(int first = 0, int second = 1)
        {
            return Task.Delay(Random.Shared.Next() % 2 == 0 ? first : second);
        }
    }
}

