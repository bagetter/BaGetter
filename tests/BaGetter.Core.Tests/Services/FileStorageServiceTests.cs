using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using BaGetter.Core.Storage;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Services;

public class FileStorageServiceTests
{
    public class GetAsync : FactsBase
    {
        [Fact]
        public async Task ThrowsIfStorePathDoesNotExist()
        {
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
                Target.GetAsync("hello.txt"));
        }

        [Fact]
        public async Task ThrowsIfFileDoesNotExist()
        {
            // Ensure the store path exists.
            Directory.CreateDirectory(StorePath);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                Target.GetAsync("hello.txt"));

            await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
                Target.GetAsync("hello/world.txt"));
        }

        [Fact]
        public async Task GetsStream()
        {
            // Arrange
            using (var content = StringStream("Hello world"))
            {
                await Target.PutAsync("hello.txt", content, "text/plain");
            }

            // Act
            var result = await Target.GetAsync("hello.txt");

            // Assert
            Assert.Equal("Hello world", await ToStringAsync(result));
        }

        [Fact]
        public async Task NoAccessOutsideStorePath()
        {
            foreach (var path in OutsideStorePathData)
            {
                await Assert.ThrowsAsync<ArgumentException>(async () =>
                    await Target.GetAsync(path));
            }
        }
    }

    public class GetDownloadUriAsync : FactsBase
    {
        [Fact]
        public async Task CreatesUriEvenIfDoesntExist()
        {
            var result = await Target.GetDownloadUriAsync("test.txt");
            var expected = new Uri(Path.Combine(StorePath, "test.txt"));

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task NoAccessOutsideStorePath()
        {
            foreach (var path in OutsideStorePathData)
            {
                await Assert.ThrowsAsync<ArgumentException>(async () =>
                    await Target.GetDownloadUriAsync(path));
            }
        }
    }

    public class PutAsync : FactsBase
    {
        [Fact]
        public async Task SavesContent()
        {
            StoragePutResult result;
            using (var content = StringStream("Hello world"))
            {
                result = await Target.PutAsync("test.txt", content, "text/plain");
            }

            var path = Path.Combine(StorePath, "test.txt");

            Assert.True(File.Exists(path));
            Assert.Equal(StoragePutResult.Success, result);
            Assert.Equal("Hello world", await File.ReadAllTextAsync(path));
        }

        [Fact]
        public async Task ReturnsAlreadyExistsIfContentAlreadyExists()
        {
            // Arrange
            var path = Path.Combine(StorePath, "test.txt");

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "Hello world");

            StoragePutResult result;
            using (var content = StringStream("Hello world"))
            {
                // Act
                result = await Target.PutAsync("test.txt", content, "text/plain");
            }

            // Assert
            Assert.Equal(StoragePutResult.AlreadyExists, result);
        }

        [Fact]
        public async Task ReturnsConflictIfContentAlreadyExistsButContentsDoNotMatch()
        {
            // Arrange
            var path = Path.Combine(StorePath, "test.txt");

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "Hello world");

            StoragePutResult result;
            using (var content = StringStream("foo bar"))
            {
                // Act
                result = await Target.PutAsync("test.txt", content, "text/plain");
            }

            // Assert
            Assert.Equal(StoragePutResult.Conflict, result);
        }

        [Fact]
        public async Task NoAccessOutsideStorePath()
        {
            foreach (var path in OutsideStorePathData)
            {
                using var content = StringStream("Hello world");
                await Assert.ThrowsAsync<ArgumentException>(async () =>
                    await Target.PutAsync(path, content, "text/plain"));
            }
        }
    }

    public class DeleteAsync : FactsBase
    {
        [Fact]
        public async Task DoesNotThrowIfPathDoesNotExist()
        {
            await Target.DeleteAsync("test.txt");
        }

        [Fact]
        public async Task Deletes()
        {
            // Arrange
            var path = Path.Combine(StorePath, "test.txt");

            Directory.CreateDirectory(StorePath);
            await File.WriteAllTextAsync(path, "Hello world");

            // Act & Assert
            await Target.DeleteAsync("test.txt");

            Assert.False(File.Exists(path));
        }

        [Fact]
        public async Task NoAccessOutsideStorePath()
        {
            foreach (var path in OutsideStorePathData)
            {
                await Assert.ThrowsAsync<ArgumentException>(async () =>
                    await Target.DeleteAsync(path));
            }
        }
    }

    public class FactsBase : IDisposable
    {
        protected readonly string StorePath;
        protected readonly Mock<IOptionsSnapshot<FileSystemStorageOptions>> Options;
        protected readonly FileStorageService Target;

        public FactsBase()
        {
            StorePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Options = new Mock<IOptionsSnapshot<FileSystemStorageOptions>>();

            Options
                .Setup(o => o.Value)
                .Returns(() => new FileSystemStorageOptions { Path = StorePath });

            Target = new FileStorageService(Options.Object);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(StorePath, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        protected Stream StringStream(string input)
        {
            var bytes = Encoding.ASCII.GetBytes(input);

            return new MemoryStream(bytes);
        }

        protected async Task<string> ToStringAsync(Stream input)
        {
            using var reader = new StreamReader(input);
            return await reader.ReadToEndAsync();
        }

        public IEnumerable<string> OutsideStorePathData
        {
            get
            {
                var fullPath = Path.GetFullPath(StorePath);
                yield return "../file";
                yield return ".";
                yield return $"../{Path.GetFileName(StorePath)}";
                yield return $"../{Path.GetFileName(StorePath)}suffix";
                yield return $"../{Path.GetFileName(StorePath)}suffix/file";
                yield return fullPath;
                yield return fullPath + Path.DirectorySeparatorChar;
                yield return fullPath + Path.DirectorySeparatorChar + "..";
                yield return fullPath + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "file";
                yield return Path.GetPathRoot(StorePath);
                yield return Path.Combine(Path.GetPathRoot(StorePath), "file");
            }
        }
    }
}
