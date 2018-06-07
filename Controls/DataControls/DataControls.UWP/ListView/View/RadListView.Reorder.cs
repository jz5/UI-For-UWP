using System.Linq;
using Telerik.Data.Core;
using Telerik.Data.Core.Layouts;
using Telerik.UI.Xaml.Controls.Data.ListView;
using Telerik.UI.Xaml.Controls.Primitives.DragDrop;
using Telerik.UI.Xaml.Controls.Primitives.DragDrop.Reorder;
using Windows.UI.Xaml;

namespace Telerik.UI.Xaml.Controls.Data
{
    /// <summary>
    /// Represents a RadListView control.
    /// </summary>
    public partial class RadListView : IReorderItemsHost
    {
        /// <summary>
        /// Identifies the <see cref="ReorderMode"/> dependency property. 
        /// </summary>
        public static readonly DependencyProperty ReorderModeProperty =
            DependencyProperty.Register(nameof(ReorderMode), typeof(ListViewReorderMode), typeof(RadListView), new PropertyMetadata(ListViewReorderMode.Default, OnReorderModeChanged));

        /// <summary>
        /// Identifies the <see cref="IsItemReorderEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsItemReorderEnabledProperty =
            DependencyProperty.Register(nameof(IsItemReorderEnabled), typeof(bool), typeof(RadListView), new PropertyMetadata(false, OnIsItemReorderEnabledChanged));

        private ReorderItemsCoordinator reorderCoordinator;

        /// <summary>
        /// Gets or sets a value indicating whether the reorder functionality is enabled.
        /// </summary>
        public bool IsItemReorderEnabled
        {
            get
            {
                return (bool)this.GetValue(IsItemReorderEnabledProperty);
            }
            set
            {
                this.SetValue(IsItemReorderEnabledProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the reorder mode of the <see cref="RadListView"/>.
        /// </summary>
        public ListViewReorderMode ReorderMode
        {
            get { return (ListViewReorderMode)GetValue(ReorderModeProperty); }
            set { SetValue(ReorderModeProperty, value); }
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The element at the specified index.</returns>
        IReorderItem IReorderItemsHost.ElementAt(int index)
        {
            // TODO refactor to optimize
            return this.childrenPanel.Children.OfType<IReorderItem>().FirstOrDefault(c => c.LogicalIndex == index);
        }

        void IReorderItemsHost.OnItemsReordered(IReorderItem sourceItem, IReorderItem destinationItem)
        {
        }

        void IReorderItemsHost.CommitReorderOperation(int sourceIndex, int destinationIndex)
        {
            var layout = this.model.layoutController.Layout;

            if (this.GroupDescriptors.Count > 0)
            {
                GroupInfo sourceInfo;
                GroupInfo destinationInfo;
                int sourceOffset;
                int destinationOffset;

                if (layout.TryGetGroupInfo(sourceIndex, out sourceInfo, out sourceOffset) &&
                    layout.TryGetGroupInfo(destinationIndex, out destinationInfo, out destinationOffset))
                {
                    var sourceGroup = (Group)sourceInfo.Item;
                    var destinationGroup = (Group)destinationInfo.Item;
                    var sourcePosition = sourceIndex - sourceOffset - 1;
                    var destinationPosition = destinationIndex - destinationOffset - 1;

                    var item = sourceGroup.Items[sourcePosition];

                    sourceGroup.RemoveItem(sourcePosition, item, null);
                    layout.RemoveItem(sourceGroup, item, sourcePosition);
                    destinationGroup.InsertItem(destinationPosition, item, null);
                    layout.AddItem(destinationGroup, item, destinationPosition);
                }
            }
            else
            {
                var dataProvider = this.model.CurrentDataProvider;
                var dataGroup = (Group)dataProvider.Results.Root.RowGroup;
                var item = dataGroup.Items[sourceIndex];

                dataGroup.RemoveItem(sourceIndex, item, null);
                layout.RemoveItem(null, item, sourceIndex);
                dataGroup.InsertItem(destinationIndex, item, null);
                layout.AddItem(null, item, destinationIndex);
            }

            this.updateService.RegisterUpdate((int)UpdateFlags.AllButData);
        }

        internal void PrepareReorderItem(RadListViewItem reorderItem)
        {
            if (reorderItem != null)
            {
                (reorderItem as IReorderItem).Coordinator = this.reorderCoordinator;
                DragDrop.SetAllowDrop(reorderItem, true);
            }
        }

        internal void CleanupReorderItem(RadListViewItem reorderItem)
        {
            if (reorderItem != null)
            {
                (reorderItem as IReorderItem).Coordinator = null;
                DragDrop.SetAllowDrop(reorderItem, false);
            }
        }

        private static void OnIsItemReorderEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listView = d as RadListView;
            if (listView != null && e.NewValue != e.OldValue)
            {
                listView.updateService.RegisterUpdate((int)UpdateFlags.AffectsData);
            }
        }

        private static void OnReorderModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listView = d as RadListView;
            if (listView != null && e.NewValue != e.OldValue)
            {
                listView.updateService.RegisterUpdate((int)UpdateFlags.AffectsData);
            }
        }

        private void InitializeReorder()
        {
            this.reorderCoordinator = new ReorderItemsCoordinator(this);
        }
    }
}