using EmailBugTracker.Logic;
using FluentAssertions;
using NUnit.Framework;

namespace EmailBugTracker.Tests
{
    public class EmailAnonymizationTests
    {
        [TestCase("john.doe@example.com", "jo****@ex****.com")]
        [TestCase("joh@exa.com", "jo****@ex****.com")]
        [TestCase("jo@exa.com", "**@ex****.com")]
        [TestCase("joh@ex.com", "jo****@**.com")]
        [TestCase("jo@ex.com", "**@**.com")]
        [TestCase("john.doe@co.uk", "jo****@**.uk")]
        [TestCase("john.doe@web.co.uk", "jo****@we****.uk")]
        public void EnsureAnonymization(string email, string expected)
        {
            EmailAnonymization.PseudoAnonymize(email).Should().Be(expected);
        }

        [TestCase("foo")]
        [TestCase("foo@bar")]
        [TestCase("foo.bar")]
        [TestCase("foo.bar@")]
        public void InvalidEmailsShould(string invalidEmail)
        {
            EmailAnonymization.PseudoAnonymize(invalidEmail).Should().Be("invalid email address");
        }
    }
}
