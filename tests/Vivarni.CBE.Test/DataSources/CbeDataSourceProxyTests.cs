using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Vivarni.CBE.DataSources;
using Xunit;

namespace Vivarni.CBE.Test.DataSources;

public class CbeDataSourceProxyTests
{
    private readonly ILogger<CbeDataSourceProxy> _logger;
    private readonly Mock<ICbeDataSource> _mockSource;
    private readonly Mock<ICbeDataSourceCache> _mockCache;

    public CbeDataSourceProxyTests()
    {
        _logger = new Mock<ILogger<CbeDataSourceProxy>>().Object;
        _mockSource = new Mock<ICbeDataSource>();
        _mockCache = new Mock<ICbeDataSourceCache>();
    }

    #region GetOpenDataFilesAsync Tests

    [Fact]
    public async Task GetOpenDataFilesAsync_WithBothSources_ShouldCombineResults()
    {
        // Arrange
        var sourceFiles = new List<CbeOpenDataFile>
        {
            new("KboOpenData_0001_2025_01_01_Full.zip"),
            new("KboOpenData_0002_2025_01_02_Update.zip")
        };
        var cacheFiles = new List<CbeOpenDataFile>
        {
            new("KboOpenData_0002_2025_01_02_Update.zip"), // Duplicate
            new("KboOpenData_0003_2025_01_03_Full.zip")
        };

        _mockSource.Setup(s => s.GetOpenDataFilesAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(sourceFiles);
        _mockCache.Setup(c => c.GetOpenDataFilesAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(cacheFiles);

        var proxy = new CbeDataSourceProxy(_logger, _mockSource.Object, _mockCache.Object);

        // Act
        var result = await proxy.GetOpenDataFilesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Count); // Should deduplicate
        Assert.Contains(result, f => f.Filename == "KboOpenData_0001_2025_01_01_Full.zip");
        Assert.Contains(result, f => f.Filename == "KboOpenData_0002_2025_01_02_Update.zip");
        Assert.Contains(result, f => f.Filename == "KboOpenData_0003_2025_01_03_Full.zip");
    }

    [Fact]
    public async Task GetOpenDataFilesAsync_WithSourceOnly_ShouldReturnSourceResults()
    {
        // Arrange
        var sourceFiles = new List<CbeOpenDataFile>
        {
            new("KboOpenData_0001_2025_01_01_Full.zip")
        };

        _mockSource.Setup(s => s.GetOpenDataFilesAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(sourceFiles);

        var proxy = new CbeDataSourceProxy(_logger, _mockSource.Object, null);

        // Act
        var result = await proxy.GetOpenDataFilesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("KboOpenData_0001_2025_01_01_Full.zip", result[0].Filename);
    }

    [Fact]
    public async Task GetOpenDataFilesAsync_WithCacheOnly_ShouldReturnCacheResults()
    {
        // Arrange
        var cacheFiles = new List<CbeOpenDataFile>
        {
            new("KboOpenData_0001_2025_01_01_Full.zip")
        };

        _mockCache.Setup(c => c.GetOpenDataFilesAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(cacheFiles);

        var proxy = new CbeDataSourceProxy(_logger, null, _mockCache.Object);

        // Act
        var result = await proxy.GetOpenDataFilesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("KboOpenData_0001_2025_01_01_Full.zip", result[0].Filename);
    }

    #endregion

    #region ReadAsync Tests

    [Fact]
    public async Task ReadAsync_WithCacheAvailable_ShouldPreferCache()
    {
        // Arrange
        var file = new CbeOpenDataFile("KboOpenData_0001_2025_01_01_Full.zip");
        using var cacheStream = new MemoryStream();
        using var sourceStream = new MemoryStream();

        _mockCache.Setup(c => c.ReadAsync(file, TestContext.Current.CancellationToken))
            .ReturnsAsync(cacheStream);
        _mockSource.Setup(s => s.ReadAsync(file, TestContext.Current.CancellationToken))
            .ReturnsAsync(sourceStream);

        var proxy = new CbeDataSourceProxy(_logger, _mockSource.Object, _mockCache.Object);

        // Act
        var result = await proxy.ReadAsync(file, TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(cacheStream, result);
        _mockCache.Verify(c => c.ReadAsync(file, TestContext.Current.CancellationToken), Times.Once);
        _mockSource.Verify(s => s.ReadAsync(file, TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task ReadAsync_WithCacheFailure_ShouldFallbackToSource()
    {
        // Arrange
        var file = new CbeOpenDataFile("KboOpenData_0001_2025_01_01_Full.zip");
        using var sourceStream = new MemoryStream();

        _mockCache.Setup(c => c.ReadAsync(file, TestContext.Current.CancellationToken))
            .ThrowsAsync(new FileNotFoundException("Cache file not found"));
        _mockSource.Setup(s => s.ReadAsync(file, TestContext.Current.CancellationToken))
            .ReturnsAsync(sourceStream);

        var proxy = new CbeDataSourceProxy(_logger, _mockSource.Object, _mockCache.Object);

        // Act
        var result = await proxy.ReadAsync(file, TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(sourceStream, result);
        _mockCache.Verify(c => c.ReadAsync(file, TestContext.Current.CancellationToken), Times.Once);
        _mockSource.Verify(s => s.ReadAsync(file, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReadAsync_WithOnlySource_ShouldUseSource()
    {
        // Arrange
        var file = new CbeOpenDataFile("KboOpenData_0001_2025_01_01_Full.zip");
        using var sourceStream = new MemoryStream();

        _mockSource.Setup(s => s.ReadAsync(file, TestContext.Current.CancellationToken))
            .ReturnsAsync(sourceStream);

        var proxy = new CbeDataSourceProxy(_logger, _mockSource.Object, null);

        // Act
        var result = await proxy.ReadAsync(file, TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(sourceStream, result);
        _mockSource.Verify(s => s.ReadAsync(file, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReadAsync_WithOnlyCache_ShouldUseCache()
    {
        // Arrange
        var file = new CbeOpenDataFile("KboOpenData_0001_2025_01_01_Full.zip");
        using var cacheStream = new MemoryStream();

        _mockCache.Setup(c => c.ReadAsync(file, TestContext.Current.CancellationToken))
            .ReturnsAsync(cacheStream);

        var proxy = new CbeDataSourceProxy(_logger, null, _mockCache.Object);

        // Act
        var result = await proxy.ReadAsync(file, TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(cacheStream, result);
        _mockCache.Verify(c => c.ReadAsync(file, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReadAsync_WithBothSourcesFailure_ShouldThrowAggregateException()
    {
        // Arrange
        var file = new CbeOpenDataFile("KboOpenData_0001_2025_01_01_Full.zip");
        var cacheException = new FileNotFoundException("Cache file not found");
        var sourceException = new InvalidOperationException("Source unavailable");

        _mockCache.Setup(c => c.ReadAsync(file, TestContext.Current.CancellationToken))
            .ThrowsAsync(cacheException);
        _mockSource.Setup(s => s.ReadAsync(file, TestContext.Current.CancellationToken))
            .ThrowsAsync(sourceException);

        var proxy = new CbeDataSourceProxy(_logger, _mockSource.Object, _mockCache.Object);

        // Act & Assert
        var aggregateException = await Assert.ThrowsAsync<AggregateException>(() =>
            proxy.ReadAsync(file, TestContext.Current.CancellationToken));

        Assert.Equal(2, aggregateException.InnerExceptions.Count);
        Assert.Contains(cacheException, aggregateException.InnerExceptions);
        Assert.Contains(sourceException, aggregateException.InnerExceptions);
        Assert.StartsWith("Failed to read the OpenDataFile. See inner exceptions for more details.", aggregateException.Message);
    }

    #endregion
}
