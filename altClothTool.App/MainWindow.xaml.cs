﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using static altClothTool.App.ClothData;

namespace altClothTool.App
{
    public partial class MainWindow : Window
    {
        public const string GTAV_DLCS_PATH = @"F:\Lié à FiveM et GTA V\vanilla_clothes_dlcs";
        private static TextBlock _statusTextBlock;
        private static ProgressBar _statusProgress;
        private static ClothData _selectedCloth;

        public static ProjectBuild ProjectBuildWindow;
        public static ObservableCollection<ClothData> Clothes;

        public MainWindow()
        {
            InitializeComponent();

            _statusTextBlock = (TextBlock)FindName("currentStatusBar");
            _statusProgress = (ProgressBar)FindName("currentProgress");

            Clothes = new ObservableCollection<ClothData>();
            clothesListBox.ItemsSource = Clothes;
        }

        public static void SetStatus(string status)
        {
            _statusTextBlock.Text = status;
        }

        public static void SetProgress(double progress)
        {
            if (progress > 1)
                progress = 1;
            if (progress < 0)
                progress = 0;

            _statusProgress.Value = _statusProgress.Maximum * progress;
        }

        private void AddMaleClothes_Click(object sender, RoutedEventArgs e)
        {
            ClothesManager.Instance().AddClothesDialog(Sex.Male);
        }

        private void AddFemaleClothes_Click(object sender, RoutedEventArgs e)
        {
            ClothesManager.Instance().AddClothesDialog(Sex.Female);
        }

        private void AddFolderClothes_Click(object sender, RoutedEventArgs e)
        {
            ClothesManager.Instance().AddClothesFolderDialog();
        }

        private void RemoveUnderCursor_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth == null) 
                return;
            var removedClothName = _selectedCloth.Name;

            var clothes = Clothes.OrderBy(x => x.Name, new AlphanumericComparer()).ToList();
            Clothes.Clear();
            foreach(var cloth in clothes.Where(c => c != _selectedCloth))
            {
                Clothes.Add(cloth);
            }

            _selectedCloth = null;
            editGroupBox.Visibility = Visibility.Collapsed;
            clothEditWindow.Visibility = Visibility.Collapsed;
            pedPropEditWindow.Visibility = Visibility.Collapsed;
            StatusController.SetStatus($"Removed '{removedClothName}'. Total clothes: " + Clothes.Count);
        }

        private void ClothesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) 
                return;
            _selectedCloth = (ClothData)e.AddedItems[0];

            if (_selectedCloth == null) 
                return;

            clothEditWindow.Visibility = Visibility.Collapsed;
            pedPropEditWindow.Visibility = Visibility.Collapsed;
            editGroupBox.Visibility = Visibility.Visible;

            if (_selectedCloth.IsComponent())
            {
                clothEditWindow.Visibility = Visibility.Visible;
                drawableName.Text = _selectedCloth.Name;

                texturesList.ItemsSource = _selectedCloth.Textures;
                firstPersonModelPath.Text = _selectedCloth.FirstPersonModelPath != "" ? _selectedCloth.FirstPersonModelPath : "Not selected...";

                unkFlag1Check.IsChecked = _selectedCloth.PedComponentFlags.unkFlag1;
                unkFlag2Check.IsChecked = _selectedCloth.PedComponentFlags.unkFlag2;
                unkFlag3Check.IsChecked = _selectedCloth.PedComponentFlags.unkFlag3;
                unkFlag4Check.IsChecked = _selectedCloth.PedComponentFlags.unkFlag4;
                isHighHeelsCheck.IsChecked = _selectedCloth.PedComponentFlags.isHighHeels;

                PostfixUCheck.IsChecked = _selectedCloth.IsPostfix_U();
                PostfixRCheck.IsChecked = !_selectedCloth.IsPostfix_U();

            }
            else
            {
                pedPropEditWindow.Visibility = Visibility.Visible;
                drawableName.Text = _selectedCloth.Name;
                pedPropName.Text = _selectedCloth.Name;

                pedPropTexturesList.ItemsSource = _selectedCloth.Textures;

                pedPropFlag1.IsChecked = _selectedCloth.PedPropFlags.unkFlag1;
                pedPropFlag2.IsChecked = _selectedCloth.PedPropFlags.unkFlag2;
                pedPropFlag3.IsChecked = _selectedCloth.PedPropFlags.unkFlag3;
                pedPropFlag4.IsChecked = _selectedCloth.PedPropFlags.unkFlag4;
                pedPropFlag5.IsChecked = _selectedCloth.PedPropFlags.unkFlag5;
            }
        }

        private void DrawableName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(_selectedCloth != null)
            {
                _selectedCloth.Name = drawableName.Text;
            }
        }

        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            Clothes.Clear();
        }

        private void OpenProjectButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "altV cloth JSON (*.altv-cloth.json)|*.altv-cloth.json",
                FilterIndex = 1,
                DefaultExt = "altv-cloth.json"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            foreach (string filename in openFileDialog.FileNames)
            {
                ProjectManager.LoadProject(filename);
            }
        }

        private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "altV cloth JSON (*.altv-cloth.json)|*.altv-cloth.json",
                FilterIndex = 1,
                DefaultExt = "altv-cloth.json"
            };

            if (saveFileDialog.ShowDialog() != true) 
                return;

            foreach (string filename in saveFileDialog.FileNames)
            {
                ProjectManager.SaveProject(filename);
            }
        }
        private void FixStaleYtdsButton_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<ClothData.Sex, List<string>> foundYddsSex = new Dictionary<ClothData.Sex, List<string>>();
            foundYddsSex.Add(Sex.Male, new List<string>());
            foundYddsSex.Add(Sex.Female, new List<string>());

            foreach (string staleYtdPath in ClothesManager.StaleYtds)
            {
                string dlc = Path.GetDirectoryName(staleYtdPath).Split('\\').Last();
                string ytdName = Path.GetFileNameWithoutExtension(staleYtdPath);
                ClothData.Sex sex = dlc.StartsWith("mp_m") ? ClothData.Sex.Male : ClothData.Sex.Female;

                string yddPath = FindYddFromStaleYtd(dlc, ytdName);

                if (yddPath != null)
                {
                    string ytdParentFolder = Directory.GetParent(staleYtdPath).ToString();
                    string yddCopyPath = Path.Combine(ytdParentFolder, Path.GetFileName(yddPath));

                    // Prevent duplicating YDD entries in found YDDs + don't copy the YDD again in case we already processed it
                    if (!File.Exists(yddCopyPath))
                    {
                        File.Copy(yddPath, yddCopyPath);
                        foundYddsSex[sex].Add(yddCopyPath);
                    }
                }
            }

            foreach (KeyValuePair<ClothData.Sex, List<string>> kp in foundYddsSex)
            {
                ClothesManager.Instance().AddClothes(kp.Key, kp.Value.ToArray());
            }
        }

        private string FindYddFromStaleYtd(string dlc, string ytdName)
        {
            string[] yddParts = ytdName.Split('_');
            string postfix = yddParts[0] != "p" ? yddParts[4].Split('.')[0] == "uni" ? "u" : "r" : "";
            string ydd = yddParts[0] == "p" ? $"p_{yddParts[1]}_{yddParts[3]}.ydd" : yddParts[0] + "_" + yddParts[2] + "_" + postfix + ".ydd";
            string path = $"{GTAV_DLCS_PATH}\\{dlc}\\{ydd}";

            if (File.Exists(path))
            {
                return path;
            } else
            {
                System.Console.WriteLine($"[WARN] Stale YDD in {path} doesn't exist");
                return null;
            }
        }

        private void AddTexture_Click(object sender, RoutedEventArgs e)
        {
            if(_selectedCloth != null)
                ClothesManager.Instance().AddClothTextures(_selectedCloth);
        }

        private void RemoveTexture_Click(object sender, RoutedEventArgs e)
        {
            if(texturesList.SelectedItem != null)
            {
                ((ObservableCollection<string>)texturesList.ItemsSource).Remove((string)texturesList.SelectedItem);
            }
        }

        private void BuildProjectButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectBuildWindow = new ProjectBuild();
            ProjectBuildWindow.Show();
        }

        private void UnkFlag1Check_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedComponentFlags.unkFlag1 = unkFlag1Check.IsChecked.GetValueOrDefault(false);
        }

        private void UnkFlag2Check_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedComponentFlags.unkFlag2 = unkFlag2Check.IsChecked.GetValueOrDefault(false);
        }

        private void UnkFlag3Check_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedComponentFlags.unkFlag3 = unkFlag3Check.IsChecked.GetValueOrDefault(false);
        }

        private void UnkFlag4Check_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedComponentFlags.unkFlag4 = unkFlag4Check.IsChecked.GetValueOrDefault(false);
        }

        private void IsHighHeelsCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedComponentFlags.isHighHeels = isHighHeelsCheck.IsChecked.GetValueOrDefault(false);
        }

        private void ClearFirstPersonModel_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.FirstPersonModelPath = "";
            firstPersonModelPath.Text = "Not selected...";
        }

        private void SelectFirstPersonModel_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                ClothesManager.Instance().SetClothFirstPersonModel(_selectedCloth);
            firstPersonModelPath.Text = !string.IsNullOrEmpty(_selectedCloth?.FirstPersonModelPath) ? _selectedCloth.FirstPersonModelPath : "Not selected...";
        }

        private void PedPropName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedCloth != null)
            {
                _selectedCloth.Name = drawableName.Text;
            }
        }

        private void PedPropFlag1_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedPropFlags.unkFlag1 = unkFlag1Check.IsChecked.GetValueOrDefault(false);
        }

        private void PedPropFlag2_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedPropFlags.unkFlag2 = unkFlag1Check.IsChecked.GetValueOrDefault(false);
        }

        private void PedPropFlag3_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedPropFlags.unkFlag3 = unkFlag1Check.IsChecked.GetValueOrDefault(false);
        }

        private void PedPropFlag4_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedPropFlags.unkFlag4 = unkFlag1Check.IsChecked.GetValueOrDefault(false);
        }

        private void PedPropFlag5_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
                _selectedCloth.PedPropFlags.unkFlag5 = unkFlag1Check.IsChecked.GetValueOrDefault(false);
        }

        private void PostfixUCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
            {
                _selectedCloth.SetCustomPostfix("u");
            }
        }

        private void PostfixRCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedCloth != null)
            {
                _selectedCloth.SetCustomPostfix("r");
            }
        }
    }
}
