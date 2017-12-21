using System;
using System.Collections.Generic;

namespace OneDrivePhotoBrowser
{

    public class CategoryManager
    {
        List<String> DefaultCategories = new List<string> { "Liked", "Disliked", "Kids", "Family", "Pets", "Travel"};

        List<Category> Categories;

        public CategoryManager()
        {
            Categories = new List<Category>();
        }

        public void Load(/*FileLocation???*/)
        {
            Categories.Clear();
            //TODO - for now load locally
            foreach(String cat in DefaultCategories)
            {
                Category newCategory = new Category(cat);
                // TODO add any mock IDs Here!!!
                // TODO add some mock SubCategories here!!!

                Categories.Add(newCategory);
            }
        }

        public List<String> GetCategory(String categoryName)
        {
            // TODO - can we load specific category only?

            List<String> result = new List<string>();

            return result;
        }
    }


    public class Category
    {
        String Name;
        List<Category> SubCategories;
        List<String> IDs;

        public Category(String name)
        {
            Name = name;
            IDs = new List<string>();
            SubCategories = new List<Category>();
        }
    }
}