namespace OneDrivePhotoBrowser.Models
{
    using System.ComponentModel;
    using System.Linq;
    using Windows.UI.Xaml.Media.Imaging;
    using Microsoft.Graph;

    public class ItemModel : INotifyPropertyChanged
    {
        private BitmapSource bitmap;
        private int imageCount = 0;

        public ItemModel(DriveItem item)
        {
            this.Item = item;
        }

        public BitmapSource Bitmap
        {
            get
            {
                return this.bitmap;
            }
            set
            {
                this.bitmap = value;
                OnPropertyChanged("Bitmap");
            }
        }

        public string Icon
        {
            get
            {
                if (this.Item.Folder != null)
                {
                    return "ms-appx:///assets/app/folder.png";
                }
                else if (this.SmallThumbnail != null)
                {
                    return this.SmallThumbnail.Url;
                }

                return null;
            }
        }

        public string Id
        {
            get
            {
                return this.Item == null ? null : this.Item.Id;
            }
        }

        public DriveItem Item { get; private set; }

        public string Name
        {
            get
            {
                return this.Item.Name;
            }
        }

        public string Location
        {
            get
            {
                if (this.Item.Location != null)
                    return this.Item.Location.ToString();
                else
                    return string.Empty;
            }
        }

        public string PathInCloud
        {
            get
            {
                return this.Item.ParentReference.Path;
            }
        }

        public string ParentFolder
        {
            get
            {
                return this.Item.ParentReference.Name;
            }
        }

        public string TakenDateTime
        {
            get
            {
                if (this.Item.Location != null)
                    return ((System.DateTimeOffset)this.Item.Photo.TakenDateTime).ToString();
                else
                    return string.Empty;
            }
        }

        public string CameraMake
        {
            get
            {
                return this.Item.Photo.CameraMake;
            }
        }

        public int ImageWidth        {
            get
            {
                return (int)this.Item.Image.Width;
            }
        }

        public int ImageHeight
        {
            get
            {
                return (int)this.Item.Image.Height;
            }
        }

        public string CameraModel
        {
            get
            {
                return this.Item.Photo.CameraMake;
            }
        }

        public Microsoft.Graph.Thumbnail SmallThumbnail
        {
            get
            {
                if (this.Item != null && this.Item.Thumbnails != null)
                {
                    var thumbnailSet = this.Item.Thumbnails.FirstOrDefault();
                    if (thumbnailSet != null)
                    {
                        return thumbnailSet.Small;
                    }
                }

                return null;
            }
        }

        //INotifyPropertyChanged members
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (null != PropertyChanged)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
