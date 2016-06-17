﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Shell;

namespace TerrainEditor.UserControls
{
    internal class DefaultResourceProvider : IResourceInfoProvider
    {
        public Type ResourceType => typeof(object);
        public bool CanCreateNew => false;
        public string[] Extensions => new string[0];

        public Task ShowEditor(FileInfo info, object resource)
        {
            Process.Start(info.FullName);
            return Task.CompletedTask;
        }
        public void SaveToDisk(FileInfo info, object resource) {}
        public object ReloadFromDisk(FileInfo info, object resource)
        {
            return resource;
        }
        public ImageSource GetPreview(FileInfo info)
        {
            using (ShellFile shellFile = ShellFile.FromFilePath(info.FullName))
            {
                var mediumBitmapSource = shellFile.Thumbnail.MediumBitmapSource;
                mediumBitmapSource.Freeze();
                return mediumBitmapSource;
            }
        }
    }
}