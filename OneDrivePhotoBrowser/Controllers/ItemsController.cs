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

namespace OneDrivePhotoBrowser.Controllers
{

    using Models;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using System;

    public class ItemsController
    {

        private GraphServiceClient graphClient;

        public ItemsController(GraphServiceClient graphClient)
        {
            this.graphClient = graphClient;
        }


        // Method that gets only children (top level) folders of a folder
        public async Task<ObservableCollection<ItemModel>> GetChildrenFolders(string id)
        {
            ObservableCollection<ItemModel> results = new ObservableCollection<ItemModel>();

            IEnumerable<DriveItem> items;

            var expandString = "children($select=Id, name, Folder)";

            // If id isn't set, get the OneDrive root's photos and folders. Otherwise, get those for the specified item ID.
            // Also retrieve the thumbnails for each item if using a consumer client.
            var itemRequest = string.IsNullOrEmpty(id)
                ? this.graphClient.Me.Drive.Root.Request().Expand(expandString)
                : this.graphClient.Me.Drive.Items[id].Request().Expand(expandString); ;
            var item = await itemRequest.GetAsync();
            items = item.Children == null
            ? new List<DriveItem>()
            : item.Children.CurrentPage.Where(child => child.Folder != null);

            foreach (var child in items)
            {
                results.Add(new ItemModel(child));
            }

            return results;
        }



        public async Task GetRemainingImages(string id, Queue<string> upcomingImageIds)
        {
            IEnumerable<DriveItem> items;
            List<string> NewImageIds = new List<string>();

            var expandString = "children($select=Id, name, Folder)";

            var itemRequest = string.IsNullOrEmpty(id)
                ? this.graphClient.Me.Drive.Root.Request().Expand(expandString)
                : this.graphClient.Me.Drive.Items[id].Request().Expand(expandString);

            var item = await itemRequest.GetAsync();
                items = item.Children == null
                ? new List<DriveItem>()
                : item.Children.CurrentPage.Where(child => child.Folder != null);

            foreach (var child in items)
            {

                NewImageIds = await GetAllImagesFromFolder(child.Id);
               
                foreach (string imageId in NewImageIds)
                {
                    upcomingImageIds.Enqueue(imageId);
                }

                await GetRemainingImages(child.Id, upcomingImageIds);
            }
        }

        /// <summary>
        /// Returns all image ids from specified folder
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task <List<string>> GetAllImagesFromFolder(string id)
        {
            List<string> results = new List<string>();

            var itemRequest = string.IsNullOrEmpty(id)
            ? this.graphClient.Me.Drive.Root.Children.Request()
            : this.graphClient.Me.Drive.Items[id].Children.Request();
            
            var items = await itemRequest.GetAsync();

            foreach (var child in items)
            {
                if (child.Image != null)
                    results.Add(child.Id);
            }

            //We need to check if there are more images, because not all childred of the folder (minimages+constant) are necesarily images
            while (items.NextPageRequest != null)
            {
                items = await items.NextPageRequest.GetAsync();
                foreach (var child in items)
                {
                    if (child.Image != null)
                        results.Add(child.Id);
                }
            }

            return results;
        }


        // Returns only minimal number of images under a certan tree structure
        // Also updates Completed folders in order to know which folders should not be revisited while looking for more images later
        public async Task<List<string>> GetInitialSetOfImagesIds(string id, int minImages)
        {
            List<string> results = new List<string>();


            // look at top level folders first
            ObservableCollection<ItemModel> folders = await GetChildrenFolders(id);
            foreach (ItemModel folder in folders)
            {
                if (results.Count < minImages)
                {
                    results.AddRange(await GetImages(folder.Id, minImages));
                }
                else
                    break;
            }

            // if we don't have enough images yet, repeat RECURSIVELY for each folder and its subfolders until image limit reached
            if (results.Count < minImages)
            {
                foreach (ItemModel folder in folders)
                {
                    if (results.Count < minImages)
                    {
                        //recursive call looking for remaining count of images (Count-minImages)
                        results.AddRange(await GetInitialSetOfImagesIds(folder.Id, results.Count - minImages));
                    }
                }
            }

            return results;
        }




        /// <summary>
        /// Get minimum number of images from a specified folder (id)
        /// If all images from the folder have been fetched with that call add folder to CompletedFolders
        /// </summary>
        /// <param name="id"></param>
        /// <param name="minImages"></param>
        /// <param name="CompletedFolders"></param>
        /// <returns></returns>
        public async Task<List<string>> GetImages(string id, int minImages)
        {
            List<string> results = new List<string>();

            var itemRequest = string.IsNullOrEmpty(id)
            ? this.graphClient.Me.Drive.Root.Children.Request()
            : this.graphClient.Me.Drive.Items[id].Children.Request();

            // TODO maybe - replace hard coded number 100 with constant value 
            var items = await itemRequest.Top(100).GetAsync();

            foreach (var child in items)
            {
                if (child.Image != null)
                    results.Add(child.Id);
            }

            //We need to check if there are more images, because not all childred of the folder (minimages+constant) are necesarily images
            while (results.Count<minImages)
            {
                if (items.NextPageRequest == null)
                    break;

                items = await items.NextPageRequest.GetAsync();
                foreach (var child in items)
                {
                    if (child.Image != null)
                        results.Add(child.Id);
                }
            }

            return results;
        }



        /// <summary>
        /// Gets the specified Image DriveItem
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ItemModel> GetImage(string id)
        {
            ObservableCollection<ItemModel> result = new ObservableCollection<ItemModel>();
            var itemRequest = this.graphClient.Me.Drive.Items[id].Request();
            var item = await itemRequest.GetAsync();     

            return new ItemModel(item);
        }
    }
}
