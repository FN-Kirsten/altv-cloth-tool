using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace altClothTool.App
{
    internal class ClothesManager
    {
        private static ClothesManager _singleton;
        private static List<string> _staleYtds = new List<string>();
        private static List<string> _allClothesTextures = new List<string>();

        public static List<string> StaleYtds { get => _staleYtds; set => _staleYtds = value; }
        public static List<string> AllClothesTextures { get => _allClothesTextures; set => _allClothesTextures = value; }

        public static ClothesManager Instance()
        {
            return _singleton ?? (_singleton = new ClothesManager());
        }

        public void AddClothesDialog(ClothData.Sex targetSex)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "Clothes geometry (*.ydd)|*.ydd",
                FilterIndex = 1,
                DefaultExt = "ydd",
                Multiselect = true,
                Title = "Adding " + (targetSex == ClothData.Sex.Male ? "male" : "female") + " clothes"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            AddClothes(targetSex, openFileDialog.FileNames);
        }

        public void AddClothesFolderDialog()
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() != true)
                return;

            string[] subFolders = Directory.GetDirectories(folderBrowserDialog.SelectedPath);

            foreach (string subFolder in subFolders)
            {
                // Get all files and YTDs in subfolder 
                string[] files = Directory.GetFiles(subFolder);
                string[] ydds = files.Where(f => f.Split('\\').Last().Contains(".ydd")).ToArray();
                string[] ytds = files.Where(f => f.Split('\\').Last().Contains(".ytd")).ToArray();
                ClothData.Sex sex = subFolder.Split('\\').Last().StartsWith("mp_m") ? ClothData.Sex.Male : ClothData.Sex.Female;

                AddClothes(sex, ydds);

                StaleYtds.AddRange(ytds.Except(AllClothesTextures));
            }
        }

        public void AddClothes(ClothData.Sex targetSex, string[] fileNames)
        {
            // Clear previously-stored cloth textures
            AllClothesTextures.Clear();

            foreach (string filename in fileNames)
            {
                string baseFileName = Path.GetFileName(filename);
                ClothNameResolver cData = new ClothNameResolver(baseFileName);

                if (cData.IsVariation)
                {
                    StatusController.SetStatus($"Item {baseFileName} can't be added. Looks like it's variant of another item");
                    continue;
                }

                ClothData nextCloth = new ClothData(filename, cData.ClothType, cData.DrawableType, cData.BindedNumber, cData.Postfix, targetSex);

                if (cData.ClothType == ClothNameResolver.ClothTypes.Component)
                {
                    nextCloth.SearchForFirstPersonModel();
                    nextCloth.SearchForTextures();

                    var clothes = MainWindow.Clothes.ToList();
                    clothes.Add(nextCloth);
                    clothes = clothes.OrderBy(x => x.Name, new AlphanumericComparer()).ToList();
                    MainWindow.Clothes.Clear();

                    foreach (var cloth in clothes)
                    {
                        MainWindow.Clothes.Add(cloth);
                    }

                    AllClothesTextures.AddRange(nextCloth.Textures);

                    StatusController.SetStatus(nextCloth + " added (" +
                                               (!string.IsNullOrEmpty(nextCloth.FirstPersonModelPath)
                                                   ? "FP Model found, "
                                                   : "") + "Found " + nextCloth.Textures.Count +
                                               " textures). Total clothes: " + MainWindow.Clothes.Count);
                }
                else
                {
                    nextCloth.SearchForTextures();

                    var clothes = MainWindow.Clothes.ToList();
                    clothes.Add(nextCloth);
                    clothes = clothes.OrderBy(x => x.Name, new AlphanumericComparer()).ToList();
                    MainWindow.Clothes.Clear();

                    foreach (var cloth in clothes)
                    {
                        MainWindow.Clothes.Add(cloth);
                    }

                    AllClothesTextures.AddRange(nextCloth.Textures);

                    StatusController.SetStatus(nextCloth + " added. (Found " + nextCloth.Textures.Count +
                                               " textures). Total clothes: " + MainWindow.Clothes.Count);
                }
            }
        }

        public void AddClothTextures(ClothData cloth)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "Clothes texture (*.ytd)|*.ytd",
                FilterIndex = 1,
                DefaultExt = "ytd",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true) 
                return;

            foreach (string filename in openFileDialog.FileNames.Where(f => f.EndsWith(".ytd")))
            {
                cloth.AddTexture(filename);
            }
        }

        public void SetClothFirstPersonModel(ClothData cloth)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "Clothes drawable (*.ydd)|*.ydd",
                FilterIndex = 1,
                DefaultExt = "ydd",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true) 
                return;

            foreach (string filename in openFileDialog.FileNames.Where(f => f.EndsWith(".ydd")))
            {
                cloth.SetFirstPersonModel(filename);
            }
        }
    }
}
