// ------------------------------------------------------------------------------
//  Copyright (c) 2015 Microsoft Corporation
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// ------------------------------------------------------------------------------

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace OneDrivePhotoBrowser
{
    using Models;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;
    using OneDrivePhotoBrowser.Controllers;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Numerics;
    using Windows.UI.Xaml.Media;
    using Windows.Storage;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ItemDetail : Page
    {
        private int IMAGES_FLIPVIEW_PERBATCH = 10;
        private int IMAGES_FLIPVIEW_LOADNEXT = 2;
        private ItemsController itemsController;
        private List<string> AllImageIds = new List<string>();
        private Queue<string> UpcomingImageIds = new Queue<string>();
        private ObservableCollection<ItemModel> ActiveImages = new ObservableCollection<ItemModel>();
        private ObservableCollection<ItemModel> NextImageBatch = new ObservableCollection<ItemModel>();

        private ItemModel currentItem = null;

        private DispatcherTimer slideShowTimer = new DispatcherTimer();
        private DispatcherTimer timeoutTimer = new DispatcherTimer();

        private Settings appSettings;
        private CategoryManager categoryManager;



        public ItemDetail()
        {
            this.InitializeComponent();
            this.Loaded += ItemTile_Loaded;

        }

        private async void ItemTile_Loaded(object sender, RoutedEventArgs e)
        {
            slideShowTimer.Tick += slideShowTimer_Tick;
            timeoutTimer.Tick += timeoutTimer_Tick;

            // load current app settings
            //appSettings.Load();
            //categoryManager.Load();

            if (this.itemsController == null)
            {
                this.itemsController = new ItemsController(((App)Application.Current).GraphClient);
            }

            var last = ((App)Application.Current).NavigationStack.Last();
            
            AllImageIds = await this.itemsController.GetInitialSetOfImagesIds(null, IMAGES_FLIPVIEW_PERBATCH*3);

            if (AllImageIds.Count == 0)
                return;

            ActiveImages = await PopulateImageBatch();

            if (ActiveImages.Count == 0)
                return;

            this.DataContext = ActiveImages;

            flipView_Slideshow.SelectedIndex = 0;
            
            // Yes, we will still be loading in the background, but intial n images should be in the slideshow by now 
            progressRing.IsActive = false;

            // Start the timeout timer. Once it ticks the slideshow timer starts
            ResetTimeoutTimer();

            NextImageBatch = await PopulateImageBatch();
            await AddUpcomingBatchToActive(false);

            await this.itemsController.GetRemainingImages(null, UpcomingImageIds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<ObservableCollection<ItemModel>> PopulateImageBatch()
        {
            ObservableCollection<ItemModel> result = new ObservableCollection<ItemModel>();
            Random rnd = new Random();

            for (int i = 0; i < IMAGES_FLIPVIEW_PERBATCH; i++)
            {
                int selectedImageIndex = rnd.Next(0, AllImageIds.Count-1);

                ItemModel nextRandomImage = await this.itemsController.GetImage(AllImageIds[selectedImageIndex]);
                if (nextRandomImage.Bitmap == null)
                    await LoadImage(nextRandomImage);
                if (!result.Contains(nextRandomImage))
                    result.Add(nextRandomImage);
            }

            return result;
        }


        /// <summary>
        /// Add and load next batch of images to Slideshow (FlipView), and if needed remove the batch from the beggining
        /// </summary>
        /// <param name="deleteFirstBatch"></param>
        /// <returns></returns>
        private async Task AddUpcomingBatchToActive(bool deleteFirstBatch)
        {
            int numberOfAddedImages = 0;

            if (NextImageBatch.Count == 0)
                NextImageBatch = await PopulateImageBatch();

            foreach (ItemModel image in NextImageBatch)
            {
                if (!ActiveImages.Contains(image))
                {
                    ActiveImages.Add(image);
                    if (image.Bitmap == null)
                        await LoadImage(image);
                    numberOfAddedImages++;
                }
            }

            if (deleteFirstBatch)
            {
                for (int index = 0; index < numberOfAddedImages; index++)
                {
                    if (ActiveImages.Count > 0)
                        ActiveImages.RemoveAt(0);
                }
            }

            this.DataContext = ActiveImages;

            NextImageBatch.Clear();
            NextImageBatch = await PopulateImageBatch();    
        }


        /// <summary>
        /// What happens when image is flipped in Slideshow:
        /// if we need to load next batch of images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ImageFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            flipView_Slideshow.UpdateLayout();

            if (flipView_Slideshow.SelectedIndex == IMAGES_FLIPVIEW_PERBATCH*2 - IMAGES_FLIPVIEW_LOADNEXT)
            {
                await AddUpcomingBatchToActive(true);
            }

            var flip = flipView_Slideshow as FlipView;
            var flipIndex = flip.SelectedIndex;

            ScrollViewer currentScrollViewer = findElementInItemsControlItemAtIndex(flip, flipIndex, "scrollViewer_ForImage") as ScrollViewer;
            Windows.UI.Xaml.Controls.Image currentImage = findElementInItemsControlItemAtIndex(flip, flipIndex, "image_CurrentImage") as Windows.UI.Xaml.Controls.Image;

            if (currentImage == null || currentScrollViewer == null)
                return;

            if (!(currentImage.ActualWidth > currentScrollViewer.ViewportWidth) && !(currentImage.ActualHeight > currentScrollViewer.ViewportHeight))
            {

            }
            else
            {
                // If the image is larger than the screen, zoom it out.
                var zoomFactor = (float)Math.Min(currentScrollViewer.ViewportWidth / currentImage.ActualWidth, currentScrollViewer.ViewportHeight / currentImage.ActualHeight);
                currentScrollViewer.ChangeView(0, 0, zoomFactor);
            }

            object o = flipView_Slideshow.SelectedItem;
            currentItem = o as ItemModel;
            SetCurrentImageInfo(currentItem);

            textblock_CurrentImageIndex.Text = flipView_Slideshow.SelectedIndex.ToString();
        }

        private void SetCurrentImageInfo(ItemModel currentItem)
        {
            SetTextblockString(textblock_ImageDateTaken, currentItem.TakenDateTime);
            SetTextblockString(textblock_ImageCameraMakeModel, currentItem.CameraMake + ", " + currentItem.CameraModel);
            SetTextblockString(textblock_ImageLocation, currentItem.Location);
            SetTextblockString(textblock_ImageName, currentItem.Name);
            SetTextblockString(textblock_ImagePathOnCloud, currentItem.PathInCloud);
            SetTextblockString(textblock_ImageResolution, currentItem.ImageHeight + " * " + currentItem.ImageHeight);
        }

        private void SetTextblockString(TextBlock textBl, string text)
        {
            if (text == null)
                textBl.Text = String.Empty;
            else
            {
                textBl.Text = text;
            }
        }

        /// <summary>
        /// Loads the actual Bitmap that is displayed in SlideShow for the image item
        /// </summary>
        /// <param name="item">The item to load.</param>
        /// <returns>The task to await.</returns>
        private async Task LoadImage(ItemModel item)
        {
            // Only load a detail view image for image items. Initialize the bitmap from the image content stream.
            if (item.Bitmap == null && (item.Item.Image != null))
            {
                item.Bitmap = new BitmapImage();
                GraphServiceClient client = ((App)Application.Current).GraphClient;

                using (var responseStream = await client.Me.Drive.Items[item.Id].Content.Request().GetAsync())
                {
                    var memoryStream = responseStream as MemoryStream;

                    if (memoryStream != null)
                    {
                        await item.Bitmap.SetSourceAsync(memoryStream.AsRandomAccessStream());
                    }
                    else
                    {
                        using (memoryStream = new MemoryStream())
                        {
                            await responseStream.CopyToAsync(memoryStream);
                            memoryStream.Position = 0;

                            await item.Bitmap.SetSourceAsync(memoryStream.AsRandomAccessStream());
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Setting timeout timer that measures for last user manipulation until the slideshow shoud start 
        /// </summary>
        private void ResetTimeoutTimer()
        {
            timeoutTimer.Interval = TimeSpan.FromSeconds(5);
            timeoutTimer.Start();

        }


        /// <summary>
        /// Sets up the timer that flips th eimage to the next one
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="start"></param>
        private void SetImageFlipTimer(int seconds, bool start)
        {
            slideShowTimer.Interval = TimeSpan.FromSeconds(seconds);
            if (start)
                slideShowTimer.Start();
        }


        private void StopImageFlipTimer()
        {
            slideShowTimer.Stop();
        }

        /// <summary>
        /// Flips to the next image in the Slidehow
        /// </summary>
        private void FlipImageForward()
        {
            int newIndex = flipView_Slideshow.SelectedIndex + 1;
            if (newIndex >= flipView_Slideshow.Items.Count || newIndex < 0)
            {
                newIndex = 0;
            }
            flipView_Slideshow.SelectedIndex = newIndex;
        }


        /// <summary>
        /// What happens then the Slideshow UI (Flipview is tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageFlipView_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (GalleryBar.Visibility == Visibility.Visible)
            {
                ShowSlideShowUI(false);
                ResetTimeoutTimer();
            }
            else
            {
                ShowSlideShowUI(true);
                StopImageFlipTimer();
                //ResetTimeoutTimer();
            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {

        }


        /// <summary>
        /// Shows/Hides UI around the image in the Slideshow
        /// </summary>
        /// <param name="visible"></param>
        private void ShowSlideShowUI(bool visible)
        {
            if(visible)
            {
                GalleryBar.Visibility = Visibility.Visible;
                gridImageInfo.Visibility = Visibility.Visible;

            }
            else
            {
                GalleryBar.Visibility = Visibility.Collapsed;
                gridImageInfo.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Finds visual control with specified name on the ItemsControl.
        ///
        /// </summary>
        /// <param name="itemsControl"></param>
        /// <param name="itemOfIndexToFind"></param>
        /// <param name="nameOfControlToFind"></param>
        /// <returns></returns>
        DependencyObject findElementInItemsControlItemAtIndex(ItemsControl itemsControl, int itemOfIndexToFind, string nameOfControlToFind)
        {
            if (itemOfIndexToFind < 0)
                return null;

            if (itemOfIndexToFind >= itemsControl.Items.Count) return null;

            DependencyObject depObj = null;
            object o = itemsControl.Items[itemOfIndexToFind];
            if (o != null)
            {
                var item = itemsControl.ContainerFromItem(o);
                
                if (item != null)
                {
                    depObj = getVisualTreeChild(item, nameOfControlToFind);
                    return depObj;
                }
            }
            return null;
        }

        /// <summary>
        /// Helper method for findElementInItemsControlItemAtIndex
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        DependencyObject getVisualTreeChild(DependencyObject obj, String name)
        {
            DependencyObject dependencyObject = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < childrenCount; i++)
            {
                var oChild = VisualTreeHelper.GetChild(obj, i);
                var childElement = oChild as FrameworkElement;
                if (childElement != null)
                {
                    //Code to take care of Paragraph/Run
                    if (childElement is RichTextBlock || childElement is TextBlock)
                    {
                        dependencyObject = childElement.FindName(name) as DependencyObject;
                        if (dependencyObject != null)
                            return dependencyObject;
                    }

                    if (childElement.Name == name)
                    {
                        return childElement;
                    }
                }
                dependencyObject = getVisualTreeChild(oChild, name);
                if (dependencyObject != null)
                    return dependencyObject;
            }
            return dependencyObject;
        }

        private void timeoutTimer_Tick(object sender, object e)
        {
            ShowSlideShowUI(false);

            // do the first image flip since we already timed out
            FlipImageForward();
            SetImageFlipTimer(5, true);
            timeoutTimer.Stop();

            while (UpcomingImageIds.Count > 0)
            {
                string id = UpcomingImageIds.Dequeue();
                if (!AllImageIds.Contains(id))
                    AllImageIds.Add(id);
            }
        }

        private void slideShowTimer_Tick(object sender, object e)
        {
            FlipImageForward();
            while (UpcomingImageIds.Count > 0)
            {
                AllImageIds.Add(UpcomingImageIds.Dequeue());
            }
        }
    }
}
