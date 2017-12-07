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

    public class ItemsController
    {

        private GraphServiceClient graphClient;

        public ItemsController(GraphServiceClient graphClient)
        {
            this.graphClient = graphClient;
        }

        /// <summary>
        /// Gets the child folders and photos of the specified item ID.
        /// </summary>
        /// <param name="id">The ID of the parent item.</param>
        /// <returns>The child folders and photos of the specified item ID.</returns>
        public async Task<ObservableCollection<ItemModel>> GetImagesAndFolders(string id)
        {
            ObservableCollection<ItemModel> results = new ObservableCollection<ItemModel>();

            IEnumerable<DriveItem> items;

            var expandString = "thumbnails, children($expand=thumbnails)";

            // If id isn't set, get the OneDrive root's photos and folders. Otherwise, get those for the specified item ID.
            // Also retrieve the thumbnails for each item if using a consumer client.
            var itemRequest = string.IsNullOrEmpty(id)
                ? this.graphClient.Me.Drive.Root.Request().Expand(expandString)
                : this.graphClient.Me.Drive.Items[id].Request().Expand(expandString);

            var item = await itemRequest.GetAsync();
            items = item.Children == null
                ? new List<DriveItem>()
                : item.Children.CurrentPage.Where(child => child.Folder != null || child.Image != null);

            foreach (var child in items)
            {
                results.Add(new ItemModel(child));
            }

            return results;
        }


        public async Task<ObservableCollection<ItemModel>> GetFolders(string id)
        {
            ObservableCollection<ItemModel> results = new ObservableCollection<ItemModel>();

            IEnumerable<DriveItem> items;

            var expandString = "children($select=Id, name, Folder)";

            // If id isn't set, get the OneDrive root's photos and folders. Otherwise, get those for the specified item ID.
            // Also retrieve the thumbnails for each item if using a consumer client.
            var itemRequest = string.IsNullOrEmpty(id)
                ? this.graphClient.Me.Drive.Root.Request().Expand(expandString)
                : this.graphClient.Me.Drive.Items[id].Request().Expand(expandString);
            var item = await itemRequest.GetAsync();
            items = item.Children == null
                ? new List<DriveItem>()
                : item.Children.CurrentPage.Where(child => child.Folder != null);

            foreach (var child in items)
            {
                results.Add(new ItemModel(child));
                //ObservableCollection<ItemModel> recursiveChilds = await GetFolders(child.Id);
                //foreach (var desc in recursiveChilds)
                //{
                  //  results.Add(desc);
                //}
            }
            
            return results;
        }


        public async Task<ObservableCollection<ItemModel>> GetFoldersWithImages(string id)
        {
            ObservableCollection<ItemModel> results = new ObservableCollection<ItemModel>();
            ObservableCollection<ItemModel> folders = await GetFolders(id);
            ObservableCollection<ItemModel> images;

            foreach (ItemModel folder in folders)
            {
                images = await GetImages(folder.Id);
                if (images.Count > 0)
                {
                    folder.ImageCount = images.Count;
                    results.Add(folder);
                }
            }

            return results;
        }

        public async Task<ObservableCollection<ItemModel>> GetImages(string id)
        {
            ObservableCollection<ItemModel> results = new ObservableCollection<ItemModel>();

            IEnumerable<DriveItem> items;

            var expandString = "children($select=name, Image)";

            // If id isn't set, get the OneDrive root's photos. Otherwise, get those for the specified item ID.
            // Also retrieve the thumbnails for each item if using a consumer client.
            var itemRequest = string.IsNullOrEmpty(id)
                ? this.graphClient.Me.Drive.Root.Request().Expand(expandString)
                : this.graphClient.Me.Drive.Items[id].Request().Expand(expandString);

            var item = await itemRequest.GetAsync();
            items = item.Children == null
                ? new List<DriveItem>()
                : item.Children.CurrentPage.Where(child => child.Image != null);

            foreach (var child in items)
            {
                results.Add(new ItemModel(child));
            }

            return results;
        }

        public async Task<ItemModel> GetImage(string id, int index)
        {
            ObservableCollection<ItemModel> results = new ObservableCollection<ItemModel>();

            IEnumerable<DriveItem> items;

            var expandString = "thumbnails, children($expand=thumbnails)";

            // If id isn't set, get the OneDrive root's photos. Otherwise, get those for the specified item ID.
            // Also retrieve the thumbnails for each item if using a consumer client.
            var itemRequest = string.IsNullOrEmpty(id)
                ? this.graphClient.Me.Drive.Root.Request().Expand(expandString)
                : this.graphClient.Me.Drive.Items[id].Request().Expand(expandString);

            var item = await itemRequest.GetAsync();

            items = item.Children == null
                ? new List<DriveItem>()
                : item.Children.CurrentPage.Where(child => child.Image != null);

            return new ItemModel(items.ElementAt<DriveItem>(index));
           
        }
    }
}
