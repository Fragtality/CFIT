﻿using CFIT.AppTools;
using CFIT.Installer.Product;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CFIT.Installer.UI.Behavior
{
    public class PageWelcome : IPageBehavior
    {
        public StackPanel CenterPanel { get { return ContentRef as StackPanel; } }

        public PageWelcome() : base()
        {

        }

        public override void Activate(InstallerWindow window)
        {
            base.Activate(window);
        }

        public override void Deactivate()
        {
            base.Deactivate();
        }

        protected override void SetHeader()
        {
            SetHeaderTitle();
            SetHeaderHints();
        }

        protected virtual void SetHeaderTitle()
        {
            TextBlock header = new TextBlock()
            {
                Text = $"{BaseConfig?.ProductName} Installer",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            Window?.PanelHeader?.Children?.Add(header);
        }

        protected virtual void SetHeaderHints()
        {
            if (BaseBehavior?.ShowInstallationHints == true)
            {
                var block = new TextBlock()
                {
                    Text = BaseBehavior?.FuncTextInstallationHintsHeader?.Invoke(Window),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                    Margin = new Thickness(0, 16, 0, 0),
                };
                Window?.PanelHeader?.Children?.Add(block);
            }
        }

        protected virtual void SetContentLogo()
        {
            if (!string.IsNullOrWhiteSpace(BaseBehavior?.WelcomeLogoResource))
            {
                var image = new Image()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8),
                };
                image.SetImageSourceFromPackUri(BaseBehavior?.WelcomeLogoResource);
                image.Width = BaseBehavior.WelcomeLogoWidth;
                (ContentRef as StackPanel)?.Children?.Add(image);
            }
        }

        protected override void SetContent()
        {
            var panel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
            };
            ContentRef = panel;
            SetContentLogo();
        }

        protected override void SetFooter()
        {
            SetFooterWarnings();
            SetFooterHints();
            SetFooterVersion();
        }

        protected virtual void SetFooterWarnings()
        {
            if (BaseBehavior?.ShowInstallationWarnings == true)
            {
                TextBlock header = new TextBlock();
                {
                    header.Text = $"Do NOT run the Installer or {BaseConfig?.ProductName} as Administrator!\r\n\r\nIf the Installer or {BaseConfig?.ProductName} Binary is blocked, add an Exclusion to your AV-Software / WindowsDefender!";
                    header.FontSize = 12;
                    header.FontWeight = FontWeights.DemiBold;
                    header.TextWrapping = TextWrapping.Wrap;
                    header.TextAlignment = TextAlignment.Center;
                    header.VerticalAlignment = VerticalAlignment.Center;
                    header.Width = 448;
                }
                Window?.PanelFooter?.Children?.Add(header);
            }
        }

        protected virtual void SetFooterHints()
        {
            if (BaseBehavior?.ShowInstallationHints == true && BaseDefinition?.IsProductInstalled == true)
            {
                var block = new TextBlock()
                {
                    Text = BaseBehavior?.FuncTextInstallationHintsFooter?.Invoke(Window),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Margin = new Thickness(0, 16, 0, 0),
                };
                Window?.PanelFooter?.Children?.Add(block);
            }
        }

        protected virtual void SetFooterVersion()
        {
            if (BaseBehavior?.ShowDetectedVersion == true && BaseConfig?.HasVersionFile == true)
            {
                var version = ProductVersion.GetProductVersionFromFile(BaseConfig.ProductVersionPath);
                var block = new TextBlock()
                {
                    Text = $"Installed Version: v{version?.VersionParsed?.ToString(BaseConfig.ProductVersionFields)} ({version?.Timestamp})",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 10,
                    Margin = new Thickness(0, 16, 0, 0),
                };
                Window?.PanelFooter?.Children?.Add(block);
            }
        }

        protected virtual MessageBoxResult ShowMessageBoxRemove(string text, string productname)
        {
            return MessageBox.Show($"{text}\r\n\r\nDo you want to continue?", $"Remove {productname}", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        }

        protected override void SetButtons()
        {
            SetLeftButton();
            SetRightButton();            
        }

        protected virtual string GetRemoveText()
        {
            return $"{BaseConfig?.ProductName} will be removed completely from your System.\r\n!!! Including all Settings and custom Changes !!!";
        }

        protected virtual void SetLeftButton()
        {
            if (BaseDefinition?.IsProductInstalled == true)
                Gui.SetButton(Window?.ButtonLeft, true, true, "Remove", Brushes.Red, IconLoader.Instance.LoadIcon("trash"), GetRemoveText());
            else
                Gui.SetButton(Window?.ButtonLeft, false, false);
        }

        protected virtual void SetRightButton()
        {
            if (BaseDefinition?.IsProductInstalled == true)
                Gui.SetButton(Window?.ButtonRight, true, true, "Update", Brushes.Green, IconLoader.Instance.LoadIcon("box-arrow-in-right"), $"Update the existing Installation of {BaseConfig?.ProductName}.\r\nRemoving the previous Version manually is neither required nor recommended!");
            else
                Gui.SetButton(Window?.ButtonRight, true, true, "Install", SystemColors.ControlTextBrush, IconLoader.Instance.LoadIcon("box-arrow-in-right"), $"Update the existing Installation of {BaseConfig?.ProductName}.\r\nRemoving the previous Version manually is neither required nor recommended!");
        }

        protected override void SetActions()
        {
            SetLeftAction();
            SetRightAction();            
        }

        protected virtual void SetLeftAction()
        {
            if (BaseDefinition?.IsProductInstalled == true)
            {
                Window.ActionLeft = (w) =>
                {
                    if (ShowMessageBoxRemove(GetRemoveText(), BaseConfig?.ProductName) == MessageBoxResult.Yes)
                    {
                        w.BaseConfig.Mode = SetupMode.REMOVE;
                        w.SetPage(InstallerPages.SETUP);
                    }
                };
            }
            else
                Window.ActionLeft = null;
        }

        protected virtual void SetRightAction()
        {
            if (Window?.HasPage(InstallerPages.CONFIG) == true)
            {
                Window.ActionRight = (w) =>
                {
                    w.SetPage(InstallerPages.CONFIG);
                };
            }
            else
            {
                Window.ActionRight = (w) =>
                {
                    w.SetPage(InstallerPages.SETUP);
                };
            }
        }
    }
}
