using System;
using CodeBucket.ViewControllers;
using CodeBucket.Core.ViewModels.Repositories;
using UIKit;
using CodeBucket.DialogElements;
using Humanizer;
using CodeBucket.Core.Utils;
using System.Collections.Generic;
using System.Reactive.Linq;
using BitbucketSharp.Models.V2;

namespace CodeBucket.Views.Repositories
{
    public class RepositoryView : PrettyDialogViewController
    {
        private readonly SplitButtonElement _split = new SplitButtonElement();
        private readonly SplitViewElement _split1 = new SplitViewElement(AtlassianIcon.Locked.ToImage(), AtlassianIcon.PageDefault.ToImage());
        private readonly SplitViewElement _split2 = new SplitViewElement(AtlassianIcon.Calendar.ToImage(), AtlassianIcon.Filezip.ToImage());
        private readonly SplitViewElement _split3 = new SplitViewElement(AtlassianIcon.Devtoolsrepository.ToImage(), AtlassianIcon.Devtoolsbranch.ToImage());

        public new RepositoryViewModel ViewModel
        {
            get { return (RepositoryViewModel)base.ViewModel; }
            set { base.ViewModel = value; }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            HeaderView.SetImage(null, Images.RepoPlaceholder);

			NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Action);
            NavigationItem.RightBarButtonItem.Enabled = false;

            var watchers = _split.AddButton("Watchers", "-");
            var forks = _split.AddButton("Forks", "-");
            var branches = _split.AddButton("Branches", "-");
 
            ViewModel.Bind(x => x.Branches).Subscribe(_ => Render(ViewModel.Repository));
            ViewModel.Bind(x => x.Issues).Subscribe(_ => Render(ViewModel.Repository));
            ViewModel.Bind(x => x.HasReadme).Subscribe(_ => Render(ViewModel.Repository));

            OnActivation(d => {
                d(watchers.Clicked.BindCommand(ViewModel.GoToStargazersCommand));
                d(ViewModel.Bind(x => x.Branches, true).Subscribe(x => branches.Text = x?.ToString() ?? "-"));
                d(ViewModel.Bind(x => x.Watchers, true).Subscribe(x => watchers.Text = x.HasValue ? x.ToString() : "-"));
                d(ViewModel.Bind(x => x.Forks, true).Subscribe(x => forks.Text = x.HasValue ? x.ToString() : "-"));

                d(ViewModel.Bind(x => x.Repository, true).Subscribe(x =>
                {
                    NavigationItem.RightBarButtonItem.Enabled = true;
                    Render(x);
                }));
            });
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NavigationItem.RightBarButtonItem.Clicked += ShowExtraMenu;
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            NavigationItem.RightBarButtonItem.Clicked -= ShowExtraMenu;
        }
		
        private void ShowExtraMenu(object o, EventArgs args)
        {
            var repoModel = ViewModel.Repository;
            if (repoModel == null)
                return;

            var sheet = new UIActionSheet();
			var pinButton = sheet.AddButton(ViewModel.IsPinned ? "Unpin from Slideout Menu" : "Pin to Slideout Menu");
            var forkButton = sheet.AddButton("Fork Repository");
			var showButton = sheet.AddButton("Show in Bitbucket");
            var cancelButton = sheet.AddButton("Cancel");
            sheet.CancelButtonIndex = cancelButton;
            sheet.DismissWithClickedButtonIndex(cancelButton, true);
            sheet.Dismissed += (s, e) => {
                BeginInvokeOnMainThread(() => {
                // Pin to menu
                if (e.ButtonIndex == pinButton)
                {
                    ViewModel.PinCommand.Execute(null);
                }
                else if (e.ButtonIndex == forkButton)
                {
                    ViewModel.ForkCommand.Execute(null);
                }
                // Show in Bitbucket
                else if (e.ButtonIndex == showButton)
                {
					ViewModel.GoToUrlCommand.Execute(ViewModel.HtmlUrl);
                }
                });

                sheet.Dispose();
            };

            sheet.ShowFrom(NavigationItem.RightBarButtonItem, true);
        }


        public void Render(Repository model)
        {
            if (model == null)
                return;
            
			Title = model.Name;

            var avatar = new Avatar(model.Links.Avatar.Href).ToUrl(128);
            ICollection<Section> root = new LinkedList<Section>();
            HeaderView.SubText = string.IsNullOrWhiteSpace(model.Description) ? "Updated " + model.UpdatedOn.Humanize() : model.Description;
            HeaderView.SetImage(avatar, Images.RepoPlaceholder);
            RefreshHeaderView();

            var sec1 = new Section();

            _split1.Button1.Image = model.IsPrivate ? AtlassianIcon.Locked.ToImage() : AtlassianIcon.Unlocked.ToImage();
            _split1.Button1.Text = model.IsPrivate ? "Private" : "Public";
            _split1.Button2.Text = string.IsNullOrEmpty(model.Language) ? "N/A" : model.Language;
            sec1.Add(_split1);

            _split3.Button1.Text = model.Scm.ApplyCase(LetterCasing.Title);
            _split3.Button2.Text = "Issues".ToQuantity(ViewModel.Issues.GetValueOrDefault());
            sec1.Add(_split3);

            _split2.Button1.Text = (model.UpdatedOn).ToString("MM/dd/yy");
            _split2.Button2.Text = model.Size.Bytes().ToString("#.##");
            sec1.Add(_split2);

            var owner = new StringElement("Owner", model.Owner.Username) { Image = AtlassianIcon.User.ToImage(),  Accessory = UITableViewCellAccessory.DisclosureIndicator };
            owner.Clicked.BindCommand(ViewModel.GoToOwnerCommand);
            sec1.Add(owner);

            if (model.Parent != null)
            {
                var parent = new StringElement("Forked From", model.Parent.Name) { Image = AtlassianIcon.Devtoolsfork.ToImage(),  Accessory = UITableViewCellAccessory.DisclosureIndicator };
                parent.Clicked.BindCommand(ViewModel.GoToForkParentCommand);
                sec1.Add(parent);
            }

            var events = new StringElement("Events", AtlassianIcon.Blogroll.ToImage());
            events.Clicked.BindCommand(ViewModel.GoToEventsCommand);
            var sec2 = new Section { events };

            if (model.HasWiki)
            {
                var wiki = new StringElement("Wiki", AtlassianIcon.Edit.ToImage());
                wiki.Clicked.BindCommand(ViewModel.GoToWikiCommand);
                sec2.Add(wiki);
            }

            if (model.HasIssues)
            {
                var issues = new StringElement("Issues", AtlassianIcon.Flag.ToImage());
                issues.Clicked.BindCommand(ViewModel.GoToIssuesCommand);
                sec2.Add(issues);
            }

            if (ViewModel.HasReadme)
            {
                var readme = new StringElement("Readme", AtlassianIcon.PageDefault.ToImage());
                readme.Clicked.BindCommand(ViewModel.GoToReadmeCommand);
                sec2.Add(readme);
            }

            var commits = new StringElement("Commits", AtlassianIcon.Devtoolscommit.ToImage());
            commits.Clicked.BindCommand(ViewModel.GoToCommitsCommand);

            var pullRequests = new StringElement("Pull Requests", AtlassianIcon.Devtoolspullrequest.ToImage());
            pullRequests.Clicked.BindCommand(ViewModel.GoToPullRequestsCommand);

            var source = new StringElement("Source", AtlassianIcon.Filecode.ToImage());
            source.Clicked.BindCommand(ViewModel.GoToSourceCommand);

            var sec3 = new Section { commits, pullRequests, source };
            foreach (var s in new[] { new Section { _split }, sec1, sec2, sec3 })
                root.Add(s);

            if (!String.IsNullOrEmpty(ViewModel.Repository.Website))
            {
                var website = new StringElement("Website", AtlassianIcon.Homepage.ToImage());
                website.Clicked.Select(_ => ViewModel.Repository.Website).BindCommand(ViewModel.GoToUrlCommand);
                root.Add(new Section { website });
            }

            Root.Reset(root);
        }
    }
}