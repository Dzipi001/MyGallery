using System;
using System.Collections.Generic;
using Windows.Storage;
using System.IO;
using System.Collections.ObjectModel;
using OneDrivePhotoBrowser.Models;
using Microsoft.Graph;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace OneDrivePhotoBrowser
{


    public class Settings
    {
        ApplicationDataContainer roamingSettings = null;

        public String ActiveCategory;
        public String ActiveSubCategory;
        public AppMode Mode;

        public enum AppMode { RANDOM=1, RANDOM_WITH_LIKED, LIKED_ONLY, DISLIKED, CATEGORY_ONLY, CURRENT_FOLDER, SAME_DAY, SAME_MONTH, SAME_YEAR};

        public Settings()
        {
            ActiveCategory = String.Empty;
            ActiveSubCategory = String.Empty;
            Mode = AppMode.RANDOM;
        }

        public void Load()
        {
            roamingSettings = ApplicationData.Current.RoamingSettings;

            object setting = roamingSettings.Values["ActiveCategory"];
            ActiveCategory = setting as String;

            setting = roamingSettings.Values["ActiveSubCategory"];
            ActiveSubCategory = setting as String;

            setting = roamingSettings.Values["Mode"];
            String ModeString = setting as String;
            Mode = (AppMode)Enum.Parse(typeof(AppMode), ModeString);
        }

        public void Save()
        {
            roamingSettings = ApplicationData.Current.RoamingSettings;

            roamingSettings.Values["ActiveCategory"] = ActiveCategory;
            roamingSettings.Values["ActiveSubCategory"] = ActiveSubCategory;
            roamingSettings.Values["Mode"] = Mode.ToString();
        }


        /// <summary>
        /// adds a picture to corresponding category,
        /// if it already exists in that category it won't do anything
        /// </summary>
        /// <param name="picid"></param>
        /// <param name="category"></param>
        public async void WritePicIdToFile(string picid, string category)
        {
            if (picid == null)
            { return; }
            if (category == null)
            { return; }

            string P = picid;
            string C = category;
            List<string> picids = new List<string>();
            picids.Clear();
            picids = await LoadPicInCurrentMode(C);
            if (!picids.Contains(P))
            {
                Windows.Storage.StorageFolder storageFolder =
                Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile sampleFile =
                    await storageFolder.CreateFileAsync(C + "Pic.txt",
                        Windows.Storage.CreationCollisionOption.OpenIfExists);
                await Windows.Storage.FileIO.AppendTextAsync(sampleFile, P + "\r\n");
            }

            
        }

       /// <summary>
       /// Returns a List<string> of pictures that are in current mode
       /// </summary>
       /// <param name="mode"></param>
       /// <returns></returns>
        public async Task<List<string>> LoadPicInCurrentMode(string mode)
        {
            List<string> picids = new List<string>();
            picids.Clear();
            var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = await folder.GetFileAsync(mode + "Pic.txt");
            var readFile = await Windows.Storage.FileIO.ReadLinesAsync(file);
            foreach (var line in readFile)
            {
                picids.Add(line);
            }
            return picids;
        }
    }



}