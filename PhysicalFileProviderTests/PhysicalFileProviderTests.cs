using FluentAssertions;
using Microsoft.Extensions.FileProviders;

namespace PhysicalFileProviderTests;

[Parallelizable(ParallelScope.All)]
public class PhysicalFileProviderTests
{
    private PhysicalFileProvider _activePollingFileProvider;
    private PhysicalFileProvider _pollingFileWatcherFileProvider;
    private string _currentDirectory;
    
    [SetUp]
    public void Setup()
    {
        _currentDirectory = Directory.GetCurrentDirectory();
        
        _activePollingFileProvider = new PhysicalFileProvider(_currentDirectory)
        {
            UseActivePolling = true,
            UsePollingFileWatcher = false
        };

        _pollingFileWatcherFileProvider = new PhysicalFileProvider(_currentDirectory)
        {
            UseActivePolling = false,
            UsePollingFileWatcher = true
        };
    }
    
    [TearDown]
    public void TearDown()
    {
        _activePollingFileProvider.Dispose();
        _pollingFileWatcherFileProvider.Dispose();
    }

    private async Task TestWatchingFileInCurrentDirectory(
        // ReSharper disable once SuggestBaseTypeForParameter
        PhysicalFileProvider watcher,
        int delay = 100)
    {
        TestContext.WriteLine("Starting TestWatchingFileInCurrentDirectory");
        
        // Arrange
        var file = Path.Combine(_currentDirectory, $"{Guid.NewGuid()}.txt");
        var changeToken = watcher.Watch("./*.txt");
        
        TestContext.WriteLine("Monitored Directory: " + _currentDirectory);
        TestContext.WriteLine("File: " + file);
        
        // Sanity check
        changeToken.HasChanged.Should().BeFalse();
        
        // Act
        await File.WriteAllTextAsync(file, "Hello World");
        
        // Wait for the change token to be updated.
        await Task.Delay(delay);
        
        // Assert
        changeToken.HasChanged.Should().BeTrue();
        
        // Cleanup
        File.Delete(file);
    }
    
    private async Task TestWatchingFileInSubDirectory(
        // ReSharper disable once SuggestBaseTypeForParameter
        PhysicalFileProvider watcher,
        int delay = 100)
    {
        TestContext.WriteLine("Starting TestWatchingFileInSubDirectory");
        
        // Arrange
        var newDirectory = Path.Combine(_currentDirectory, Guid.NewGuid().ToString());
        
        Directory.CreateDirectory(newDirectory);
        
        var file = Path.Combine(newDirectory, $"{Guid.NewGuid()}.txt");
        var changeToken = watcher.Watch("./**/*.txt");
        
        TestContext.WriteLine("Monitored Directory: " + newDirectory);
        TestContext.WriteLine("File: " + file);
        
        // Sanity check
        changeToken.HasChanged.Should().BeFalse();
        
        // Act
        await File.WriteAllTextAsync(file, "Hello World");
        
        // Wait for the change token to be updated.
        await Task.Delay(delay);
        
        // Assert
        changeToken.HasChanged.Should().BeTrue();
        
        // Cleanup
        File.Delete(file);
        Directory.Delete(newDirectory, true);
    }

    [Test]
    public Task TestWatchingAFileInCurrentDirectoryWithActivePolling() => 
        TestWatchingFileInCurrentDirectory(_activePollingFileProvider);

    [Test]
    public Task TestWatchingAFileInSubDirectoryWithActivePolling() =>
        TestWatchingFileInSubDirectory(_activePollingFileProvider);

    [Test]
    public Task TestWatchingAFileInCurrentDirectoryWithPollingFileWatcher() =>
        TestWatchingFileInCurrentDirectory(_pollingFileWatcherFileProvider, 4500);
    
    [Test]
    public  Task TestWatchingAFileInSubDirectoryWithPollingFileWatcher() =>
        TestWatchingFileInSubDirectory(_pollingFileWatcherFileProvider, 4500);
}