using System;
using System.Collections.Generic;
using Windows.Storage;

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
    }

}