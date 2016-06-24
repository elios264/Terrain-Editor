using System.Windows;
using System.Windows.Controls;

namespace TerrainEditor.Utilities
{
    public class IndexedItemsControl : ItemsControl
    {
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is IndexedContentPresenter;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new IndexedContentPresenter(this);
        }
    }
    public class IndexedContentPresenter : ContentPresenter
    {
        private readonly ItemsControl m_parent;

        public int Index => m_parent.ItemContainerGenerator.IndexFromContainer(this);

        public IndexedContentPresenter(ItemsControl parent)
        {
            m_parent = parent;
        }
    }


}