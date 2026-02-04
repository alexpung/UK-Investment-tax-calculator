using Xunit;

namespace UnitTest.Test.TradeCalculations;

[CollectionDefinition("NonParallelTests", DisableParallelization = true)]
public class NonParallelTestsCollection : ICollectionFixture<object>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
