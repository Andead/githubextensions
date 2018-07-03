using System;
using Moq;
using Xunit;

namespace Tools.Github.Extensions.Tests
{
    public class ExtensionsServiceTests
    {
        [Theory]
        [InlineData("test", "test", 0)]
        [InlineData(null, null, 0)]
        public void Autocomplete_CallsAutocompleteService(string owner, string repo, int number)
        {
            var mock = new Mock<IAutocompleteService>();

            ExtensionsService service = new ExtensionsService(mock.Object);

            // Act
            service.Autocomplete(owner, repo, number);

            // Assert
            mock.Verify(s => s.Run(owner, repo, number), Times.Once);
        }
    }
}