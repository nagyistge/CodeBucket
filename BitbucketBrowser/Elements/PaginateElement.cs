using System;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using CodeFramework.UI.Views;

namespace BitbucketBrowser.Elements
{
    public class PaginateElement : LoadMoreElement
    {
        static PaginateElement()
        {
            Padding = 20;
        }

        public PaginateElement(string normal, string loading, Action<LoadMoreElement> tap)
            : base(normal, loading, tap)
        {
            Font = StyledElement.DefaultTitleFont;
            TextColor = StyledElement.DefaultTitleColor;
        }

        protected override void CellCreated(UITableViewCell cell, UIView view)
        {
            base.CellCreated(cell, view);
            cell.BackgroundView = new CellBackgroundView();
        }
    }
}
