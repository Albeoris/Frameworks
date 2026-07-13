using Albeoris.Games.Core.NsCapacityCalculator;
using Xunit;

namespace Albeoris.Games.Core.Tests.NsCapacityCalculator;

/// <summary>
/// Contains unit tests for the <see cref="CapacityCalculator"/> class.
/// </summary>
public class CapacityCalculatorTests
{
    /// <summary>
    /// Verifies that a valid offset between two boundaries returns true and calculates the correct capacity.
    /// </summary>
    [Fact]
    public void TryGetCapacity_ReturnsTrueAndCalculatesCapacity_WhenOffsetIsBetweenBoundaries()
    {
        // Arrange
        var calculator = new CapacityCalculator();
        calculator.RegisterBoundary(0);
        calculator.RegisterBoundary(100);

        // Act
        var result = calculator.TryGetCapacity(50, out var capacity);

        // Assert
        Assert.True(result);
        Assert.Equal(50, capacity); // from offset 50 to next boundary (100)
    }

    /// <summary>
    /// Verifies boundaries are inserted in ascending order when registering.
    /// </summary>
    [Fact]
    public void RegisterBoundary_InsertsBoundariesInAscendingOrder()
    {
        // Arrange
        var calculator = new CapacityCalculator();

        // Act
        calculator.RegisterBoundary(50);
        calculator.RegisterBoundary(10);
        calculator.RegisterBoundary(80);

        // Assert
        // Quick check: boundary 10 is first, 50 is second, 80 is third
        // We'll rely on TryGetCapacity to indirectly confirm ordering for now
        Assert.True(calculator.TryGetCapacity(10, out var capacity1));
        Assert.True(calculator.TryGetCapacity(50, out var capacity2));

        // capacity from offset 10 to next boundary (which is 50)
        Assert.Equal(40, capacity1);

        // capacity from offset 50 to next boundary (which is 80)
        Assert.Equal(30, capacity2);
    }

    /// <summary>
    /// Checks that if no boundaries are registered, <see cref="TryGetCapacity(long,out long)"/> returns false and 0 capacity.
    /// </summary>
    [Fact]
    public void TryGetCapacity_ReturnsFalse_WhenNoBoundaries()
    {
        // Arrange
        var calculator = new CapacityCalculator();

        // Act
        var result = calculator.TryGetCapacity(10, out var capacity);

        // Assert
        Assert.False(result);
        Assert.Equal(0, capacity);
    }

    /// <summary>
    /// Checks that if the offset is beyond the last boundary, the method returns false and 0 capacity.
    /// </summary>
    [Fact]
    public void TryGetCapacity_ReturnsFalse_WhenOffsetExceedsLastBoundary()
    {
        // Arrange
        var calculator = new CapacityCalculator();
        calculator.RegisterBoundary(10);
        calculator.RegisterBoundary(20);

        // Act
        Boolean result = calculator.TryGetCapacity(30, out var capacity);

        // Assert
        Assert.False(result);
        Assert.Equal(0, capacity);
    }

    /// <summary>
    /// Checks that registering a negative boundary throws an <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void RegisterBoundary_ThrowsArgumentOutOfRangeException_WhenBoundaryIsNegative()
    {
        // Arrange
        var calculator = new CapacityCalculator();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.RegisterBoundary(-1));
    }

    /// <summary>
    /// Checks that registering the same boundary twice throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void RegisterBoundary_ThrowsInvalidOperationException_WhenBoundaryAlreadyExists()
    {
        // Arrange
        var calculator = new CapacityCalculator();
        calculator.RegisterBoundary(100);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => calculator.RegisterBoundary(100));
    }
    
    [Fact]
    public void UnregisterBoundary_ThrowsArgumentOutOfRangeException_WhenBoundaryIsNegative()
    {
        // Arrange
        var calculator = new CapacityCalculator();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.UnregisterBoundary(-10));
    }

    [Fact]
    public void UnregisterBoundary_ThrowsInvalidOperationException_WhenBoundaryNotFound()
    {
        // Arrange
        var calculator = new CapacityCalculator();
        calculator.RegisterBoundary(50);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => calculator.UnregisterBoundary(99));
    }

    [Fact]
    public void UnregisterBoundary_RemovesBoundarySuccessfully()
    {
        // Arrange
        var calculator = new CapacityCalculator();
        calculator.RegisterBoundary(10);
        calculator.RegisterBoundary(20);

        // Act
        calculator.UnregisterBoundary(20);

        // Assert
        Assert.False(calculator.TryGetCapacity(10, out _));
    }

    [Fact]
    public void ClearBoundaries_RemovesAllBoundaries()
    {
        // Arrange
        var calculator = new CapacityCalculator();
        calculator.RegisterBoundary(10);
        calculator.RegisterBoundary(100);

        // Act
        calculator.ClearBoundaries();
        var result = calculator.TryGetCapacity(0, out _);

        // Assert
        Assert.False(result);
    }
}