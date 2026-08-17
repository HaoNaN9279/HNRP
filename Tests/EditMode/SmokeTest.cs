// <copyright file="SmokeTest.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Smoke tests for the EditMode test infrastructure.
    /// </summary>
    public sealed class SmokeTest
    {
        /// <summary>
        /// Verifies the test assembly is discovered and can execute tests.
        /// </summary>
        [Test]
        public void TestAssembly_Exists()
        {
            Assert.IsTrue(true);
        }
    }
}
