﻿using BiliLite.Api.User;
using BiliLite.Helpers;
using System;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliLite.Dialogs
{
    public sealed partial class EditFavFolderDialog : ContentDialog
    {
        readonly FavoriteApi favoriteApi;
        readonly string id;
        public EditFavFolderDialog(string id,string title,string desc,bool isOpen)
        {
            this.InitializeComponent();
            favoriteApi = new FavoriteApi();
            this.id = id;
            txtTitle.Text = title;
            txtDesc.Text = desc;
            checkPrivacy.IsChecked = isOpen;
        }
        public bool Success { get; set; } = false;
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrEmpty(txtTitle.Text))
            {
                Utils.ShowMessageToast("请输入收藏夹名称");
                return;
            }
            try
            {
                IsPrimaryButtonEnabled = false;
                var result = await favoriteApi.EditFavorite(txtTitle.Text, txtDesc.Text, checkPrivacy.IsChecked.Value,id).Request();
                if (result.status)
                {
                    var data = await result.GetData<object>();
                    if (data.success)
                    {
                        Utils.ShowMessageToast("修改成功");
                        Success = true;
                        this.Hide();
                    }
                    else
                    {
                        Utils.ShowMessageToast(data.message);
                    }

                }
                else
                {
                    Utils.ShowMessageToast(result.message);
                }
               

            }
            catch (Exception ex)
            {
                Utils.ShowMessageToast(ex.Message);
            }
            finally
            {
                IsPrimaryButtonEnabled = true;
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide();
        }

    }
}
