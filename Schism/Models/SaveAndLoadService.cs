using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace Schism.Models
{
    public class SaveAndLoadService
    {
        
        public SaveAndLoadService()
        {
            // Constructor logic here
        }

        public void Save(SaveData sD)
        {
            // Logic to save data
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Schism");
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            string json = JsonSerializer.Serialize(new
            {
                sD.SaveDeviceID,
                sD.SaveStartAddress,
                sD.SaveLength,
                sD.SaveDataType,
                sD.SaveNumericBase,
                sD.SaveEndian,
                sD.SaveASCIIEnable,
                sD.SaveADisplayType
            });

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "userData"; // Default file name
            saveFileDialog.DefaultExt = ".sav"; // Default file extension
                                                // Filter files by extension. The format is "Description|Pattern"
            saveFileDialog.Filter = "Schism Save File (.sav)|*.sav|All files (*.*)|*.*";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // Show save file dialog box
            bool? result = saveFileDialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = saveFileDialog.FileName;

                // Example of saving text from a TextBox named 'txtEditor'
                try
                {
                    File.WriteAllText(filename, json);
                    MessageBox.Show("File saved successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}");
                }
            }

        }
        public SaveData Load()
        {
            SaveData sD = new SaveData();

            var openFileDialog = new OpenFileDialog();

            // Optional: Configure the dialog box
            openFileDialog.FileName = "userData"; // Default file name
            openFileDialog.DefaultExt = ".sav"; // Default file extension
            openFileDialog.Filter = "Schism Save File (.sav)|*.sav|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Initial Directory

            // Show open file dialog box
            bool? result = openFileDialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (data != null)
                {
                    sD.SaveLength = Convert.ToInt32(data["SaveLength"]);
                    sD.SaveStartAddress = Convert.ToInt32(data["SaveStartAddress"]);
                    sD.SaveDeviceID = Convert.ToInt32(data["SaveDeviceID"]);
                    sD.SaveASCIIEnable = Convert.ToBoolean(data["SaveASCIIEnable"]);
                    sD.SaveDataType = data.TryGetValue("SaveDataType", out var saveDataType) && saveDataType != null
                        ? Convert.ToString(saveDataType)!
                        : string.Empty;
                    sD.SaveNumericBase = data.TryGetValue("SaveNumericBase", out var saveNumericBase) && saveNumericBase != null
                        ? Convert.ToString(saveNumericBase)!
                        : string.Empty;
                    sD.SaveEndian = data.TryGetValue("SaveEndian", out var saveEndian) && saveEndian != null
                        ? Convert.ToString(saveEndian)!
                        : string.Empty;
                    sD.SaveADisplayType = data.TryGetValue("SaveADisplayType", out var saveADisplayType) && saveADisplayType != null
                        ? Convert.ToString(saveADisplayType)!
                        : string.Empty;
                }
            }
            return sD;
        }
    }
}
