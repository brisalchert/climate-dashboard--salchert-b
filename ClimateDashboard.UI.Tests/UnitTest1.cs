namespace ClimateDashboard.UI.Tests;

using Bunit;
using Pages;

public class CounterTests : BunitContext
{
  [Fact]
  public void CounterShouldIncrementWhenButtonClicked()
  {
    // Arrange: Render the component
    var cut = Render<Counter>();

    // Act: Find the button and click it
    cut.Find("button").Click();

    // Assert: Check if the text updated
    cut.Find("p").MarkupMatches("<p role=\"status\">Current count: 1</p>");
  }
}
