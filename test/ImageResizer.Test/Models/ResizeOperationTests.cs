using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using ImageResizer.Properties;
using ImageResizer.Test;
using Xunit;

namespace ImageResizer.Models
{
    public class ResizeOperationTests : IDisposable
    {
        readonly TestDirectory _directory = new TestDirectory();

        [Fact]
        public void Execute_copies_frame_metadata()
        {
            var operation = new ResizeOperation("Test.jpg", _directory, Settings());

            operation.Execute();

            AssertEx.Image(
                _directory.File(),
                image => Assert.Equal("Brice Lambson", ((BitmapMetadata)image.Frames[0].Metadata).Author[0]));
        }

        [Fact]
        public void Execute_keeps_date_modified()
        {
            var operation = new ResizeOperation("Test.jpg", _directory, Settings(s => s.KeepDateModified = true));

            operation.Execute();

            Assert.Equal(File.GetLastWriteTimeUtc("Test.jpg"), File.GetLastWriteTimeUtc(_directory.File()));
        }

        [Fact]
        public void Execute_replaces_originals()
        {
            var path = Path.Combine(_directory, "Test.jpg");
            File.Copy("Test.jpg", path);

            var operation = new ResizeOperation(path, null, Settings(s => s.Replace = true));

            operation.Execute();

            AssertEx.Image(_directory.File(), image => Assert.Equal(96, image.Frames[0].PixelWidth));
        }

        [Fact]
        public void Execute_uniquifies_output_filename()
        {
            File.WriteAllBytes(Path.Combine(_directory, "Test (Test).jpg"), new byte[0]);

            var operation = new ResizeOperation("Test.jpg", _directory, Settings());

            operation.Execute();

            Assert.Contains("Test (Test) (1).jpg", _directory.FileNames);
        }

        [Fact]
        public void Execute_uniquifies_output_filename_again()
        {
            File.WriteAllBytes(Path.Combine(_directory, "Test (Test).jpg"), new byte[0]);
            File.WriteAllBytes(Path.Combine(_directory, "Test (Test) (1).jpg"), new byte[0]);

            var operation = new ResizeOperation("Test.jpg", _directory, Settings());

            operation.Execute();

            Assert.Contains("Test (Test) (2).jpg", _directory.FileNames);
        }

        [Fact]
        public void Execute_uses_fileName_format()
        {
            var operation = new ResizeOperation(
                "Test.jpg",
                _directory,
                Settings(s => s.FileName = "%1_%2_%3_%4_%5_%6"));

            operation.Execute();

            Assert.Contains("Test_Test_96_96_96_96.jpg", _directory.FileNames);
        }

        [Fact]
        public void Execute_transforms_each_frame()
        {
            var operation = new ResizeOperation("Test1.gif", _directory, Settings());

            operation.Execute();

            AssertEx.Image(
                _directory.File(),
                image =>
                {
                    Assert.Equal(2, image.Frames.Count);
                    AssertEx.All(image.Frames, frame => Assert.Equal(96, frame.PixelWidth));
                });
        }

        [Fact]
        public void Execute_uses_fallback_encoder()
        {
            var operation = new ResizeOperation(
                "Test.ico",
                _directory,
                Settings(s => s.FallbackEncoder = new TiffBitmapEncoder().CodecInfo.ContainerFormat));

            operation.Execute();

            Assert.Contains("Test (Test).tiff", _directory.FileNames);
        }

        // TODO
        // transforms
        //     ignores orientation
        //     converts units
        //     shrinks only
        //     uses fit
        //         crops when fill

        public void Dispose()
            => _directory.Dispose();

        Settings Settings(Action<Settings> action = null)
        {
            var settings = new Settings
            {
                Sizes = new ObservableCollection<ResizeSize>
                {
                    new ResizeSize
                    {
                        Name = "Test",
                        Width = 96,
                        Height = 96
                    }
                },
                SelectedSizeIndex = 0
            };
            action?.Invoke(settings);

            return settings;
        }
    }
}
